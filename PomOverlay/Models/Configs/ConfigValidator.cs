using ColorConverter = System.Windows.Media.ColorConverter;

namespace PomOverlay
{
    /// <summary>
    /// 手編集された config.json の壊れた値を、物理演算に流れ込む前に安全な値へ置き換える
    /// </summary>
    public static class ConfigValidator
    {
        private const double DefaultTransitionSec = 30.0;

        /// <summary>
        /// config をその場で修正し、修正した内容（ユーザーに見せる文言）を返す
        /// </summary>
        public static List<string> Sanitize(AppConfig config)
        {
            var warnings = new List<string>();
            var defaults = AppConfig.CreateDefault();

            if (!double.IsFinite(config.TransitionSec) || config.TransitionSec < 0)
            {
                warnings.Add($"TransitionSec が不正です ({config.TransitionSec})。{DefaultTransitionSec} を使います");
                config.TransitionSec = DefaultTransitionSec;
            }

            if (!double.IsFinite(config.Fps) || config.Fps < 1 || config.Fps > AppConfig.MaxFps)
            {
                warnings.Add($"Fps は1～{AppConfig.MaxFps}で指定してください ({config.Fps})。{AppConfig.DefaultFps} を使います");
                config.Fps = AppConfig.DefaultFps;
            }

            config.Modes ??= new();
            config.Schedules ??= new();

            foreach (var key in config.Modes.Keys.ToList())
            {
                var fallback = defaults.Modes.GetValueOrDefault(key) ?? defaults.Modes["Focus"];
                if (config.Modes[key] is null)
                {
                    warnings.Add($"Modes.{key} が空です。初期値を使います");
                    config.Modes[key] = fallback;
                    continue;
                }
                SanitizePhase(key, config.Modes[key], fallback, warnings);
            }

            // Focus と Rest がどちらも0分だとサイクル長が0になり、剰余が NaN になる
            if (config.Modes.TryGetValue("Focus", out var focus) && config.Modes.TryGetValue("Rest", out var rest)
                && focus.Min + rest.Min <= 0)
            {
                warnings.Add("Focus と Rest の Min がどちらも0です。初期値を使います");
                focus.Min = defaults.Modes["Focus"].Min;
                rest.Min = defaults.Modes["Rest"].Min;
            }

            for (int i = 0; i < config.Schedules.Count; i++)
            {
                var s = config.Schedules[i];
                if (s is null)
                {
                    warnings.Add($"Schedules[{i}] が空です。無視します");
                    continue;
                }
                if (!TimeSpan.TryParse(s.Start, out _) || !TimeSpan.TryParse(s.End, out _))
                {
                    warnings.Add($"Schedules[{i}] の時刻が読めません ({s.Start}-{s.End})。このスケジュールは無視されます");
                }
            }
            config.Schedules.RemoveAll(s => s is null);

            return warnings;
        }

        private static void SanitizePhase(string key, PhaseConfig p, PhaseConfig d, List<string> warnings)
        {
            // 0以下だとゼロ除算になる項目
            p.PulseSec = RequirePositive(key, nameof(p.PulseSec), p.PulseSec, d.PulseSec, warnings);
            p.FlowDuration = RequirePositive(key, nameof(p.FlowDuration), p.FlowDuration, d.FlowDuration, warnings);

            // 負数が意味を持たない項目（0は許容）
            if (p.Min < 0)
            {
                warnings.Add($"{key}.Min が負数です ({p.Min})。{d.Min} を使います");
                p.Min = d.Min;
            }
            p.Thick = RequireNonNegative(key, nameof(p.Thick), p.Thick, d.Thick, warnings);
            p.BlurMin = RequireNonNegative(key, nameof(p.BlurMin), p.BlurMin, d.BlurMin, warnings);
            p.BlurMax = RequireNonNegative(key, nameof(p.BlurMax), p.BlurMax, d.BlurMax, warnings);
            if (p.BlurMin > p.BlurMax)
            {
                warnings.Add($"{key} の BlurMin ({p.BlurMin}) が BlurMax ({p.BlurMax}) より大きいので入れ替えます");
                (p.BlurMin, p.BlurMax) = (p.BlurMax, p.BlurMin);
            }

            // 不透明度は 0～1
            p.OpMin = RequireUnit(key, nameof(p.OpMin), p.OpMin, d.OpMin, warnings);
            p.OpMax = RequireUnit(key, nameof(p.OpMax), p.OpMax, d.OpMax, warnings);
            if (p.OpMin > p.OpMax)
            {
                warnings.Add($"{key} の OpMin ({p.OpMin}) が OpMax ({p.OpMax}) より大きいので入れ替えます");
                (p.OpMin, p.OpMax) = (p.OpMax, p.OpMin);
            }

            // 読めない色名は描画のたびに例外になるので取り除く
            var colors = p.ColorStrings ?? Array.Empty<string>();
            var valid = colors.Where(IsValidColor).ToArray();
            foreach (var bad in colors.Except(valid))
            {
                warnings.Add($"{key}.ColorStrings の \"{bad}\" は色として読めません。取り除きます");
            }
            if (valid.Length == 0)
            {
                if (colors.Length > 0) warnings.Add($"{key}.ColorStrings に使える色がありません。初期値を使います");
                valid = d.ColorStrings;
            }
            p.ColorStrings = valid;
        }

        private static double RequirePositive(string key, string name, double value, double fallback, List<string> warnings)
        {
            if (double.IsFinite(value) && value > 0) return value;
            warnings.Add($"{key}.{name} は0より大きい必要があります ({value})。{fallback} を使います");
            return fallback;
        }

        private static double RequireNonNegative(string key, string name, double value, double fallback, List<string> warnings)
        {
            if (double.IsFinite(value) && value >= 0) return value;
            warnings.Add($"{key}.{name} が負数です ({value})。{fallback} を使います");
            return fallback;
        }

        private static double RequireUnit(string key, string name, double value, double fallback, List<string> warnings)
        {
            if (double.IsFinite(value) && value >= 0 && value <= 1) return value;
            warnings.Add($"{key}.{name} は0～1の範囲で指定してください ({value})。{fallback} を使います");
            return fallback;
        }

        private static bool IsValidColor(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            try
            {
                return ColorConverter.ConvertFromString(s) is not null;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
