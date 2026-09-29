namespace PomOverlay.Tests
{
    public class ConfigValidatorTests
    {
        [Fact]
        public void DefaultConfig_HasNoWarnings()
        {
            var config = AppConfig.CreateDefault();

            var warnings = ConfigValidator.Sanitize(config);

            Assert.Empty(warnings);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-3.0)]
        public void NonPositivePulseSecAndFlowDuration_FallBackToModeDefaults(double bad)
        {
            var config = AppConfig.CreateDefault();
            config.Modes["Rest"].PulseSec = bad;
            config.Modes["Rest"].FlowDuration = bad;

            var warnings = ConfigValidator.Sanitize(config);

            var defaults = AppConfig.CreateDefault().Modes["Rest"];
            Assert.Equal(defaults.PulseSec, config.Modes["Rest"].PulseSec);
            Assert.Equal(defaults.FlowDuration, config.Modes["Rest"].FlowDuration);
            Assert.Equal(2, warnings.Count);
        }

        [Fact]
        public void NegativeValues_FallBackToDefaults()
        {
            var config = AppConfig.CreateDefault();
            var focus = config.Modes["Focus"];
            focus.Min = -1;
            focus.Thick = -2;
            focus.BlurMin = -4;

            ConfigValidator.Sanitize(config);

            var d = AppConfig.CreateDefault().Modes["Focus"];
            Assert.Equal(d.Min, focus.Min);
            Assert.Equal(d.Thick, focus.Thick);
            Assert.Equal(d.BlurMin, focus.BlurMin);
        }

        [Fact]
        public void ZeroIsAllowedWhereItMakesSense()
        {
            var config = AppConfig.CreateDefault();
            config.Modes["Rest"].Min = 0;
            config.Modes["Focus"].Thick = 0;
            config.Modes["Focus"].BlurMin = 0;
            config.TransitionSec = 0;

            var warnings = ConfigValidator.Sanitize(config);

            Assert.Empty(warnings);
        }

        [Fact]
        public void FocusAndRestBothZeroMinutes_FallBackToDefaults()
        {
            var config = AppConfig.CreateDefault();
            config.Modes["Focus"].Min = 0;
            config.Modes["Rest"].Min = 0;

            ConfigValidator.Sanitize(config);

            Assert.Equal(25, config.Modes["Focus"].Min);
            Assert.Equal(5, config.Modes["Rest"].Min);
        }

        [Fact]
        public void InvertedRanges_AreSwapped()
        {
            var config = AppConfig.CreateDefault();
            var p = config.Modes["Focus"];
            p.BlurMin = 50; p.BlurMax = 10;
            p.OpMin = 0.9; p.OpMax = 0.3;

            ConfigValidator.Sanitize(config);

            Assert.Equal((10.0, 50.0), (p.BlurMin, p.BlurMax));
            Assert.Equal((0.3, 0.9), (p.OpMin, p.OpMax));
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(1.5)]
        public void OpacityOutsideZeroToOne_FallsBackToDefault(double bad)
        {
            var config = AppConfig.CreateDefault();
            config.Modes["Focus"].OpMax = bad;

            ConfigValidator.Sanitize(config);

            Assert.Equal(new PhaseConfig().OpMax, config.Modes["Focus"].OpMax);
        }

        [Fact]
        public void InvalidColors_AreRemoved()
        {
            var config = AppConfig.CreateDefault();
            config.Modes["Focus"].ColorStrings = ["Cyan", "NotAColor", "#12345", "#FF0000"];

            var warnings = ConfigValidator.Sanitize(config);

            Assert.Equal(["Cyan", "#FF0000"], config.Modes["Focus"].ColorStrings);
            Assert.Equal(2, warnings.Count);
        }

        [Fact]
        public void NoValidColors_FallBackToDefaultColors()
        {
            var config = AppConfig.CreateDefault();
            config.Modes["Rest"].ColorStrings = ["xxx"];

            ConfigValidator.Sanitize(config);

            Assert.Equal(AppConfig.CreateDefault().Modes["Rest"].ColorStrings, config.Modes["Rest"].ColorStrings);
        }

        [Fact]
        public void UnknownModeKey_UsesFocusDefaultsAsFallback()
        {
            var config = AppConfig.CreateDefault();
            config.Modes["Lunch"] = new PhaseConfig { Min = 60, PulseSec = 0, FlowDuration = 5, ColorStrings = ["Orange"] };

            ConfigValidator.Sanitize(config);

            Assert.Equal(AppConfig.CreateDefault().Modes["Focus"].PulseSec, config.Modes["Lunch"].PulseSec);
        }

        [Fact]
        public void NullCollectionsAndEntries_AreReplaced()
        {
            var config = new AppConfig { Modes = null!, Schedules = null! };

            ConfigValidator.Sanitize(config);

            Assert.NotNull(config.Modes);
            Assert.NotNull(config.Schedules);

            var config2 = AppConfig.CreateDefault();
            config2.Modes["Rest"] = null!;
            config2.Schedules.Add(null!);

            ConfigValidator.Sanitize(config2);

            Assert.NotNull(config2.Modes["Rest"]);
            Assert.DoesNotContain(null, config2.Schedules);
        }

        [Fact]
        public void UnparsableScheduleTime_IsReportedButKept()
        {
            var config = AppConfig.CreateDefault();
            config.Schedules.Add(new ScheduleItem { Start = "25:99", End = "26:00", ApplyMode = AppConfig.Mode.Rest });

            var warnings = ConfigValidator.Sanitize(config);

            Assert.Single(warnings);
            Assert.Contains("25:99", warnings[0].Problem);
            Assert.EndsWith("このスケジュールは無視されます", warnings[0].ToString());
            Assert.Equal(3, config.Schedules.Count);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-30.0)]
        [InlineData(0.5)]
        [InlineData(61.0)]
        [InlineData(double.PositiveInfinity)]
        public void FpsOutsideOneToSixty_FallsBackToDefault(double bad)
        {
            var config = AppConfig.CreateDefault();
            config.Fps = bad;

            var warnings = ConfigValidator.Sanitize(config);

            Assert.Equal(AppConfig.DefaultFps, config.Fps);
            Assert.Single(warnings);
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(15.0)]
        [InlineData(60.0)]
        public void FpsWithinRange_IsKept(double fps)
        {
            var config = AppConfig.CreateDefault();
            config.Fps = fps;

            Assert.Empty(ConfigValidator.Sanitize(config));
            Assert.Equal(fps, config.Fps);
        }

        [Fact]
        public void NegativeTransitionSec_FallsBackToDefault()
        {
            var config = AppConfig.CreateDefault();
            config.TransitionSec = -5;

            ConfigValidator.Sanitize(config);

            Assert.Equal(30.0, config.TransitionSec);
        }

        [Fact]
        public void SanitizedConfig_ProducesFiniteStateAndPhysics()
        {
            var config = AppConfig.CreateDefault();
            foreach (var p in config.Modes.Values)
            {
                p.PulseSec = 0; p.FlowDuration = -1; p.Min = 0;
            }
            config.Schedules.Clear();

            ConfigValidator.Sanitize(config);

            var calc = new AuroraPhysicsCalculator();
            for (int i = 0; i < 100; i++)
            {
                var state = PomodoroStateCalculator.Calculate(config, new DateTime(2026, 9, 29, 10, i % 60, 0));
                var physics = calc.Update(state, 0.016);

                Assert.True(double.IsFinite(state.RemainingSec));
                Assert.True(double.IsFinite(state.ProgressRatio));
                Assert.True(double.IsFinite(physics.Blur));
                Assert.True(double.IsFinite(physics.Opacity));
                Assert.True(double.IsFinite(physics.Flow));
            }
        }
    }
}
