namespace PomOverlay.Tests
{
    public class SettingsViewModelTests
    {
        [Fact]
        public void RoundTrip_KeepsAllValues()
        {
            var source = AppConfig.CreateDefault();
            source.OverrideMode = AppConfig.Mode.Sleep;
            source.Fps = 24;

            var built = new SettingsViewModel(source).BuildConfig();

            Assert.Equal(source.ToJson(), built.ToJson());
        }

        [Fact]
        public void EditedValues_AreReflectedInBuiltConfig()
        {
            var vm = new SettingsViewModel(AppConfig.CreateDefault());
            var rest = vm.Modes.Single(m => m.Name == "Rest");
            rest.Thick = 35;
            rest.ColorsText = " Red ,Orange,, #112233 ";
            vm.TransitionSec = 10;

            var config = vm.BuildConfig();

            Assert.Equal(35, config.Modes["Rest"].Thick);
            Assert.Equal(["Red", "Orange", "#112233"], config.Modes["Rest"].ColorStrings);
            Assert.Equal(10, config.TransitionSec);
        }

        [Fact]
        public void OverrideMode_IsCarriedOverUnchanged()
        {
            var source = AppConfig.CreateDefault();
            source.OverrideMode = AppConfig.Mode.Focus;

            Assert.Equal(AppConfig.Mode.Focus, new SettingsViewModel(source).BuildConfig().OverrideMode);
        }

        [Fact]
        public void AddAndRemoveSchedule()
        {
            var vm = new SettingsViewModel(AppConfig.CreateDefault());
            int before = vm.Schedules.Count;

            var added = vm.AddSchedule();
            added.Start = "15:00"; added.End = "15:30"; added.ApplyMode = AppConfig.Mode.Sleep;
            Assert.Contains(vm.BuildConfig().Schedules, s => s.Start == "15:00" && s.ApplyMode == AppConfig.Mode.Sleep);

            vm.RemoveSchedule(vm.Schedules[0]);
            Assert.Equal(before, vm.Schedules.Count);
        }

        [Fact]
        public void TryBuildConfig_ValidValues_Succeeds()
        {
            var vm = new SettingsViewModel(AppConfig.CreateDefault());

            Assert.True(vm.TryBuildConfig(out var config, out var errors));
            Assert.Empty(errors);
            Assert.Equal(3, config.Modes.Count);
        }

        [Fact]
        public void TryBuildConfig_InvalidValues_FailsWithReasons_AndDoesNotAlterEdits()
        {
            var vm = new SettingsViewModel(AppConfig.CreateDefault());
            var focus = vm.Modes.Single(m => m.Name == "Focus");
            focus.PulseSec = 0;
            focus.ColorsText = "Cyan, NotAColor";
            vm.Schedules[0].Start = "25:00";
            vm.Fps = 500;

            Assert.False(vm.TryBuildConfig(out _, out var errors));

            Assert.Equal(4, errors.Count);
            Assert.Contains(errors, e => e.Contains("PulseSec"));
            Assert.Contains(errors, e => e.Contains("NotAColor"));
            Assert.Contains(errors, e => e.Contains("25:00"));
            Assert.Contains(errors, e => e.Contains("Fps"));
            // 入力中の値は補正で書き換えない（ユーザーが直す）
            Assert.Equal(0, focus.PulseSec);
            Assert.Equal(500, vm.Fps);
        }

        [Fact]
        public void PropertyChanged_IsRaisedOnEdit()
        {
            var mode = ModeSettingsViewModel.From("Focus", new PhaseConfig());
            var raised = new List<string?>();
            mode.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

            mode.Thick = 5;
            mode.Thick = 5; // 同じ値では通知しない

            Assert.Equal([nameof(ModeSettingsViewModel.Thick)], raised);
        }

        [Fact]
        public void ScheduleModes_ExcludeAuto()
        {
            Assert.DoesNotContain(AppConfig.Mode.Auto, SettingsViewModel.ScheduleModes);
        }
    }
}
