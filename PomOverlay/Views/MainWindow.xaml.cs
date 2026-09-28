using PomOverlay.Managers;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using static System.Windows.Forms.AxHost;
using Color = System.Windows.Media.Color;

namespace PomOverlay
{
    public partial class MainWindow : Window
    {
        private readonly string[] _spinnerFrames = { "  ", "  ", ". ", "..", ".." };
        private readonly DebugManager _debugManager = new();

        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        const int GWL_EXSTYLE = -20, WS_EX_TRANSPARENT = 0x20, WS_EX_LAYERED = 0x80000;

        public int ScreenIndex { get; set; }
        public bool IsDebugVisible => DebugContainer.Visibility == Visibility.Visible;

        private bool _isJapanese = true;
        private DateTime _lastTick = DateTime.Now;

        private AppConfig _config = new();
        private readonly AuroraPhysicsCalculator _physics = new();

        private readonly DebugLabels _labels = new();



        //// フィールドはこれ1つで済む
        //private readonly DebugLabels _labels = new DebugLabels();
        public void UpdateConfig(AppConfig config)
        {
            this._config = config;
            _labels.SetLanguage(_isJapanese);
        }



        public MainWindow(Rect bounds, int index, AppConfig config)
        {
            InitializeComponent();


            this.ScreenIndex = index;
            this._config = config; // フィールドに保持

            this.ScreenIndex = index;
            this.Left = bounds.X; this.Top = bounds.Y; this.Width = bounds.Width; this.Height = bounds.Height;

            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };//30FPS:33  60なら16に
            timer.Tick += Update;
            timer.Start();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }// 物理演算の結果をまとめて持ち運ぶためのクラス


        private void Update(object? sender, EventArgs e)
        {

            try
            {
                var now = DateTime.Now;
                double delta = (now - _lastTick).TotalSeconds;
                _lastTick = now;

                // 1. ロジック計算（時間の計算）
                var state = PomodoroStateCalculator.Calculate(_config, now);

                // 2. 物理演算（数値の補完と揺らぎ）
                var physics = _physics.Update(state, delta);

                // 3. 描画反映（WPF要素への適用）
                ApplyVisuals(physics, state);

                // 4. デバッグ表示
                if (IsDebugVisible)
                {
                    UpdateDebugText(now, state, physics, delta);
                }
            }
            catch (Exception ex)
            {
                {
                    // 1回だけログを吐いて、タイマーを止めるなどの処置
                    _debugManager.LogError("Update Loop Error", ex);
                }

            }
        }

        //private void ApplyVisuals(AuroraPhysics physics, PomodoroState state)
        //{
        //    AuroraRect.StrokeThickness = physics.Thick;
        //    BlurEff.Radius = physics.Blur;
        //    BrushTransform.X = physics.Flow;
        //    BrushTransform.Y = physics.Flow;

        //    int stopCount = AuroraBrush.GradientStops.Count;
        //    for (int i = 0; i < stopCount; i++)
        //    {
        //        Color cSrc = state.CurrentSet.GetInterpolatedColor(i, stopCount);
        //        Color cDst = state.TargetSet.GetInterpolatedColor(i, stopCount);
        //        AuroraBrush.GradientStops[i].Color = LerpColorStatic(cSrc, cDst, state.TransRatio);
        //    }
        //}
        private void ApplyVisuals(AuroraPhysics physics, PomodoroState state)
        {
            AuroraRect.StrokeThickness = physics.Thick;
            BlurEff.Radius = physics.Blur;

            // 不透明度を反映
            AuroraRect.Opacity = physics.Opacity;

            // ブラシの移動（Flow）
            BrushTransform.X = physics.Flow;
            BrushTransform.Y = physics.Flow;

            // グラデーションの色の更新
            int stopCount = AuroraBrush.GradientStops.Count;
            for (int i = 0; i < stopCount; i++)
            {
                // 最後の Stop (i == stopCount - 1) は、ループを滑らかにするために
                // 最初の色 (i == 0) と同じ色を目指すように計算する

                Color cSrc = state.CurrentSet.GetInterpolatedColor(i, stopCount);
                Color cDst = state.TargetSet.GetInterpolatedColor(i, stopCount);

                AuroraBrush.GradientStops[i].Color = Interpolation.LerpColor(cSrc, cDst, state.TransRatio);
            }
        }

        private void UpdateDebugText(DateTime now, PomodoroState s, AuroraPhysics p, double delta)
        {
            // ロジックを Manager に丸投げ
            DebugText.Text = _debugManager.GenerateDebugText(
                now, s, p, _config, ScreenIndex, _labels, delta);
        }

        //private void UpdateDebugText(DateTime now, PomodoroState s, AuroraPhysics p)
        //{
        //    const int TotalWidth = 44; // 調整しやすいように変数化
        //    string line = new string('-', TotalWidth);

        //    var sb = new StringBuilder();

        //    // 1 & 2. Header / Mode
        //    sb.AppendLine($"[ {_labels.Header}: {ScreenIndex} ] {now:HH:mm:ss}");
        //    sb.AppendLine(line);

        //    // スケジュールによる強制中かどうかのフラグ表示
        //    var scheduledMode = _config.GetCurrentMode(now);
        //    string modeString = scheduledMode.ToString().ToUpper(); // これで解決

        //    bool isForced = !string.IsNullOrEmpty(modeString);
        //    string modeType = isForced ? "[SCHEDULED]" : "[CYCLE]";
        //    sb.AppendLine($"{_labels.Mode}: {s.GetDisplayName(_isJapanese)} {modeType}");
        //    sb.AppendLine($"{_labels.Time}: {Math.Floor(s.RemainingSec / 60):00}:{Math.Floor(s.RemainingSec % 60):00}");


        //    // --- プログレスバーの組み立て ---
        //    int barWidth = 24;
        //    double prog = Math.Clamp(s.ProgressRatio, 0, 1);
        //    int filled = (int)(prog * barWidth);

        //    // スピナーのインデックス決定 (250msごとに1フレーム進む例)
        //    int spinnerIndex = (now.Millisecond / (1000 / _spinnerFrames.Length)) % _spinnerFrames.Length;
        //    string spinner = _spinnerFrames[spinnerIndex];

        //    // 中身を組み立て（常に barWidth 分の長さになる）
        //    string barContent;
        //    if (filled >= barWidth)
        //    {
        //        // 100% のときは全部埋める（または完了マーク）
        //        barContent = new string('#', barWidth);
        //    }
        //    else if (filled > 0)
        //    {
        //        // 先端をスピナーにする： [###/------]
        //        barContent = new string('#', filled - 1) + spinner + new string('-', barWidth - filled);
        //    }
        //    else
        //    {
        //        // 0% のとき： [/---------]
        //        barContent = spinner + new string('-', barWidth - 1);
        //    }

        //    sb.AppendLine($"{_labels.Progress}: [{barContent}] {prog * 100,4:0.0}%");
        //    sb.AppendLine(line);


        //    // --- 【新設】スケジュール設定の表示 ---
        //    sb.AppendLine(_isJapanese ? "▼ 登録済みスケジュール" : "▼ REGISTERED SCHEDULES");
        //    sb.Append(_config.GetScheduleSummary());
        //    sb.AppendLine();
        //    sb.AppendLine(line);

        //    // 次に「ポモドーロの周期」も表示
        //    var fMin = _config.Modes.GetValueOrDefault("Focus")?.Min ?? 0;
        //    var rMin = _config.Modes.GetValueOrDefault("Rest")?.Min ?? 0;
        //    sb.AppendLine($"  (Loop: {fMin}m / {rMin}m)");
        //    sb.AppendLine(line);

        //    // 4. Physics (分離したラベルを組み合わせて表示)
        //    // 太さ : 10.0 px / 設定 : 10.0
        //    sb.AppendLine($"{_labels.ThickTitle}: {p.Thick,5:0.0} px  / {_labels.SettingLabel}:{s.CurrentSet.Thick,12:0.0} px");

        //    // ぼかし : 20.0 px / 範囲 : 10.0-30.0
        //    sb.AppendLine($"{_labels.BlurTitle}: {p.Blur,5:0.0} px  / {_labels.RangeLabel}:{s.CurrentSet.BlurMin,5:0.0}-{s.CurrentSet.BlurMax,6:0.0} px");

        //    // 周期 :  3.0 秒 / 設定 :  3.0
        //    string secUnit = _isJapanese ? "秒" : "s ";
        //    sb.AppendLine($"{_labels.PulseTitle}: {p.PulseTime,5:0.0} {secUnit}  / {_labels.CycleLabel}:{p.PulseSec,12:0.0} {secUnit}");

        //    //不透明度
        //    sb.AppendLine($"{_labels.OpTitle}: {p.Opacity * 100,5:0.0} %   / {_labels.RangeLabel}:       {s.CurrentSet.OpMin * 100:0}-{s.CurrentSet.OpMax * 100:0} %");
        //    //sb.AppendLine($"Current Opacity: {p.Opacity:F2}");

        //    // 流速 :  15.2 秒 / 周期 : 60.0
        //    double currentFlowSec = p.Flow * s.CurrentSet.FlowDuration;
        //    sb.AppendLine($"{_labels.FlowTitle}: {currentFlowSec,5:0.0} {secUnit}  / {_labels.CycleLabel}:{s.CurrentSet.FlowDuration,12:0.0} {secUnit}");

        //    // 5. Status
        //    sb.AppendLine(line);
        //    sb.AppendLine($"{_labels.Osc}: {p.Osc * 100,5:0.0} %");

        //    //sb.AppendLine($"Opacity Range: {s.CurrentSet.OpMin:F2} - {s.CurrentSet.OpMax:F2}");
        //    //sb.AppendLine($"Current Opacity: {p.Opacity:F2}");

        //    string transText = s.GetTransitionText(_isJapanese, _transitionSec);
        //    if (!string.IsNullOrEmpty(transText)) sb.AppendLine(transText);

        //    DebugText.Text = sb.ToString();
        //}

        //public void SetLanguage(bool jp)
        //{
        //    _isJapanese = jp;
        //    _labels.SetLanguage(_isJapanese);
        //}

        public void SetLanguage(bool jp)
        {
            _isJapanese = jp;
            _labels.SetLanguage(_isJapanese);
            _debugManager.SetLanguage(jp); // Manager にも通知
        }

        public void SetDebugVisibility(bool v) => DebugContainer.Visibility = v ? Visibility.Visible : Visibility.Collapsed;
    }
}