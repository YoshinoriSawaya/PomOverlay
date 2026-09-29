using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PomOverlay
{
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// 設定ウィンドウの編集内容。AppConfig から作り、編集後に AppConfig を組み立てる。
    /// OverrideMode はトレイメニューで切り替えるものなので、ここでは編集せず元の値を引き継ぐ
    /// </summary>
    public class SettingsViewModel : ObservableObject
    {
        // スケジュールで指定できるモード（Auto はサイクルに戻るだけなので選ばせない）
        public static IReadOnlyList<AppConfig.Mode> ScheduleModes { get; } = [AppConfig.Mode.Focus, AppConfig.Mode.Rest, AppConfig.Mode.Sleep];

        private readonly AppConfig.Mode _overrideMode;
        private double _transitionSec;
        private double _fps;

        public double TransitionSec { get => _transitionSec; set => Set(ref _transitionSec, value); }
        public double Fps { get => _fps; set => Set(ref _fps, value); }
        public ObservableCollection<ModeSettingsViewModel> Modes { get; } = new();
        public ObservableCollection<ScheduleSettingsViewModel> Schedules { get; } = new();

        public SettingsViewModel(AppConfig source)
        {
            _overrideMode = source.OverrideMode;
            _transitionSec = source.TransitionSec;
            _fps = source.Fps;
            foreach (var (name, phase) in source.Modes) Modes.Add(ModeSettingsViewModel.From(name, phase));
            foreach (var s in source.Schedules) Schedules.Add(ScheduleSettingsViewModel.From(s));
        }

        public ScheduleSettingsViewModel AddSchedule()
        {
            var item = new ScheduleSettingsViewModel { Start = "12:00", End = "13:00", ApplyMode = AppConfig.Mode.Rest };
            Schedules.Add(item);
            return item;
        }

        public void RemoveSchedule(ScheduleSettingsViewModel item) => Schedules.Remove(item);

        public AppConfig BuildConfig()
        {
            var config = new AppConfig
            {
                OverrideMode = _overrideMode,
                TransitionSec = TransitionSec,
                Fps = Fps,
            };
            foreach (var m in Modes) config.Modes[m.Name] = m.ToPhase();
            foreach (var s in Schedules) config.Schedules.Add(s.ToSchedule());
            return config;
        }

        /// <summary>
        /// 編集内容から AppConfig を組み立てる。ConfigValidator が補正する値が1つでもあれば適用させず、理由を返す
        /// </summary>
        public bool TryBuildConfig(out AppConfig config, out IReadOnlyList<string> errors)
        {
            config = BuildConfig();
            // Sanitize は渡した設定を書き換えるので、複製で確かめる
            var probe = AppConfig.FromJson(config.ToJson()) ?? new AppConfig();
            // 補正の内容（「30 を使います」など）は読み込み時の話なので、ここでは問題だけを見せる
            errors = ConfigValidator.Sanitize(probe).Select(i => i.Problem).ToList();
            return errors.Count == 0;
        }
    }

    public class ModeSettingsViewModel : ObservableObject
    {
        private int _min;
        private double _thick, _blurMin, _blurMax, _opMin, _opMax, _pulseSec, _flowDuration;
        private string _colorsText = "";

        public string Name { get; init; } = "";
        public int Min { get => _min; set => Set(ref _min, value); }
        public double Thick { get => _thick; set => Set(ref _thick, value); }
        public double BlurMin { get => _blurMin; set => Set(ref _blurMin, value); }
        public double BlurMax { get => _blurMax; set => Set(ref _blurMax, value); }
        public double OpMin { get => _opMin; set => Set(ref _opMin, value); }
        public double OpMax { get => _opMax; set => Set(ref _opMax, value); }
        public double PulseSec { get => _pulseSec; set => Set(ref _pulseSec, value); }
        public double FlowDuration { get => _flowDuration; set => Set(ref _flowDuration, value); }

        // 色はカンマ区切りの1行で編集する（例: "DeepSkyBlue, Cyan, #FF0000"）
        public string ColorsText { get => _colorsText; set => Set(ref _colorsText, value); }

        public static ModeSettingsViewModel From(string name, PhaseConfig p) => new()
        {
            Name = name,
            Min = p.Min,
            Thick = p.Thick,
            BlurMin = p.BlurMin,
            BlurMax = p.BlurMax,
            OpMin = p.OpMin,
            OpMax = p.OpMax,
            PulseSec = p.PulseSec,
            FlowDuration = p.FlowDuration,
            ColorsText = string.Join(", ", p.ColorStrings ?? []),
        };

        public PhaseConfig ToPhase() => new()
        {
            Min = Min,
            Thick = Thick,
            BlurMin = BlurMin,
            BlurMax = BlurMax,
            OpMin = OpMin,
            OpMax = OpMax,
            PulseSec = PulseSec,
            FlowDuration = FlowDuration,
            ColorStrings = ParseColors(ColorsText),
        };

        public static string[] ParseColors(string text) =>
            text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    public class ScheduleSettingsViewModel : ObservableObject
    {
        private string _start = "00:00", _end = "00:00";
        private AppConfig.Mode _applyMode = AppConfig.Mode.Rest;

        public string Start { get => _start; set => Set(ref _start, value); }
        public string End { get => _end; set => Set(ref _end, value); }
        public AppConfig.Mode ApplyMode { get => _applyMode; set => Set(ref _applyMode, value); }

        public static ScheduleSettingsViewModel From(ScheduleItem s) => new() { Start = s.Start, End = s.End, ApplyMode = s.ApplyMode };

        public ScheduleItem ToSchedule() => new() { Start = Start, End = End, ApplyMode = ApplyMode };
    }
}
