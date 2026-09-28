namespace PomOverlay.Tests
{
    public class PomodoroStateCalculatorTests
    {
        // Focus 25分 / Rest 5分 = 30分サイクル。毎時 00分・30分 に Focus が始まる
        private static AppConfig CreateConfig()
        {
            var config = new AppConfig { TransitionSec = 30.0 };
            config.Modes["Focus"] = new PhaseConfig { Min = 25, Thick = 2 };
            config.Modes["Rest"] = new PhaseConfig { Min = 5, Thick = 20 };
            config.Modes["Sleep"] = new PhaseConfig { Min = 0, Thick = 1 };
            return config;
        }

        private static DateTime At(int hour, int minute, int second, int millisecond = 0)
            => new(2026, 9, 29, hour, minute, second, millisecond);

        [Theory]
        [InlineData(10, 0, 0, 0, "Focus", 1500.0)]
        [InlineData(10, 24, 59, 0, "Focus", 1.0)]
        [InlineData(10, 25, 0, 0, "Rest", 300.0)]
        [InlineData(10, 29, 59, 0, "Rest", 1.0)]
        [InlineData(10, 30, 0, 0, "Focus", 1500.0)]
        public void Cycle_SwitchesAtFocusRestBoundaries(int h, int m, int s, int ms, string expectedMode, double expectedRem)
        {
            var state = PomodoroStateCalculator.Calculate(CreateConfig(), At(h, m, s, ms));

            Assert.Equal(expectedMode, state.ModeName);
            Assert.Equal(expectedMode == "Focus", state.IsWork);
            Assert.Equal(expectedRem, state.RemainingSec, 6);
        }

        [Fact]
        public void Cycle_SetsCurrentAndTargetPhaseConfigs()
        {
            var config = CreateConfig();

            var focus = PomodoroStateCalculator.Calculate(config, At(10, 5, 0));
            Assert.Same(config.Modes["Focus"], focus.CurrentSet);
            Assert.Same(config.Modes["Rest"], focus.TargetSet);

            var rest = PomodoroStateCalculator.Calculate(config, At(10, 26, 0));
            Assert.Same(config.Modes["Rest"], rest.CurrentSet);
            Assert.Same(config.Modes["Focus"], rest.TargetSet);
        }

        [Theory]
        [InlineData(10, 24, 0, 0.0)]  // 残り60秒: フェード前
        [InlineData(10, 24, 30, 0.0)] // 残り30秒: フェード開始の直前(境界は含まない)
        [InlineData(10, 24, 45, 0.5)] // 残り15秒: 半分
        public void Cycle_TransRatioRisesDuringLastTransitionSec(int h, int m, int s, double expected)
        {
            var state = PomodoroStateCalculator.Calculate(CreateConfig(), At(h, m, s));

            Assert.Equal(expected, state.TransRatio, 6);
        }

        [Fact]
        public void Cycle_ProgressRatioReflectsElapsedTimeInPhase()
        {
            // Focus開始から5分 = 25分中の 1/5
            var state = PomodoroStateCalculator.Calculate(CreateConfig(), At(10, 5, 0));

            Assert.Equal(0.2, state.ProgressRatio, 6);
        }

        [Fact]
        public void OverrideMode_TakesPriorityOverCycle()
        {
            var config = CreateConfig();
            config.OverrideMode = AppConfig.Mode.Rest;

            // サイクル上は Focus の時刻
            var state = PomodoroStateCalculator.Calculate(config, At(10, 5, 0));

            Assert.Equal("Rest", state.ModeName);
            Assert.False(state.IsWork);
            Assert.Equal(0.0, state.TransRatio);
            Assert.Same(config.Modes["Rest"], state.CurrentSet);
            Assert.Same(config.Modes["Rest"], state.TargetSet);
        }

        [Fact]
        public void OverrideMode_TakesPriorityOverSchedule()
        {
            var config = CreateConfig();
            config.OverrideMode = AppConfig.Mode.Focus;
            config.Schedules.Add(new ScheduleItem { Start = "10:00", End = "11:00", ApplyMode = AppConfig.Mode.Sleep });

            var state = PomodoroStateCalculator.Calculate(config, At(10, 26, 0));

            Assert.Equal("Focus", state.ModeName);
            Assert.True(state.IsWork);
        }

        [Fact]
        public void Schedule_OverlappingSchedules_FirstOneWins()
        {
            var config = CreateConfig();
            config.Schedules.Add(new ScheduleItem { Start = "10:00", End = "12:00", ApplyMode = AppConfig.Mode.Sleep });
            config.Schedules.Add(new ScheduleItem { Start = "11:00", End = "13:00", ApplyMode = AppConfig.Mode.Rest });

            var state = PomodoroStateCalculator.Calculate(config, At(11, 5, 0));

            Assert.Equal("Sleep", state.ModeName);
        }

        [Theory]
        [InlineData(23, 30, "Sleep")]
        [InlineData(0, 30, "Sleep")]
        [InlineData(1, 5, "Focus")] // End は含まない → サイクルに戻る
        public void Schedule_SpanningMidnight_IsApplied(int h, int m, string expectedMode)
        {
            var config = CreateConfig();
            config.Schedules.Add(new ScheduleItem { Start = "23:00", End = "01:00", ApplyMode = AppConfig.Mode.Sleep });

            var state = PomodoroStateCalculator.Calculate(config, At(h, m, 0));

            Assert.Equal(expectedMode, state.ModeName);
        }

        [Fact]
        public void Schedule_ModeMissingFromModes_FallsBackToCycle()
        {
            var config = CreateConfig();
            config.Modes.Remove("Sleep");
            config.Schedules.Add(new ScheduleItem { Start = "10:00", End = "11:00", ApplyMode = AppConfig.Mode.Sleep });

            var state = PomodoroStateCalculator.Calculate(config, At(10, 5, 0));

            Assert.Equal("Focus", state.ModeName);
        }
    }
}
