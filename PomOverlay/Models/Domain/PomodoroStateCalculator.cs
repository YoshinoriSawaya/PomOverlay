namespace PomOverlay
{
    // 設定と現在時刻から、ポモドーロの現在モードを判定する
    public static class PomodoroStateCalculator
    {
        public static PomodoroState Calculate(AppConfig config, DateTime now)
        {
            AppConfig.Mode activeMode = config.GetCurrentMode(now);

            // Auto 以外なら強制/スケジュールモード確定
            if (activeMode != AppConfig.Mode.Auto)
            {
                string modeKey = activeMode.ToString();

                if (config.Modes.TryGetValue(modeKey, out var conf))
                {
                    return new PomodoroState
                    {
                        ModeName = modeKey,
                        IsWork = (activeMode == AppConfig.Mode.Focus),
                        RemainingSec = 0,
                        ProgressRatio = 1.0,
                        TransRatio = 0.0,
                        CurrentSet = conf,
                        TargetSet = conf
                    };
                }
            }

            // Dictionaryから安全に設定を取得（キーがない場合の保険も兼ねる）
            var focusConf = config.Modes.GetValueOrDefault("Focus") ?? new PhaseConfig { Min = 25 };
            var restConf = config.Modes.GetValueOrDefault("Rest") ?? new PhaseConfig { Min = 5 };

            double focusSec = focusConf.Min * 60.0;
            double restSec = restConf.Min * 60.0;
            double cycle = focusSec + restSec;

            // 現在のサイクル位置
            double currentCycleSec = ((now.Minute * 60) + now.Second + (now.Millisecond / 1000.0)) % cycle;

            bool isFocus = currentCycleSec < focusSec;
            string currentModeName = isFocus ? "Focus" : "Rest";
            string targetModeName = isFocus ? "Rest" : "Focus";

            double rem = isFocus ? focusSec - currentCycleSec : cycle - currentCycleSec;
            double currentPhaseMax = isFocus ? focusSec : restSec;

            return new PomodoroState
            {
                ModeName = currentModeName,
                IsWork = isFocus,
                RemainingSec = rem,
                ProgressRatio = 1.0 - (rem / currentPhaseMax),
                TransRatio = (rem < config.TransitionSec) ? (config.TransitionSec - rem) / config.TransitionSec : 0.0,
                CurrentSet = config.Modes.GetValueOrDefault(currentModeName) ?? new PhaseConfig(),
                TargetSet = config.Modes.GetValueOrDefault(targetModeName) ?? new PhaseConfig()
            };
        }
    }
}
