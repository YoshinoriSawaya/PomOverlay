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
        private double _currentPhase = 0, _currentFlow = 0, _transitionSec = 15.0;


        // 修正後
        private AppConfig _config = new();
        private string _currentModeName = "Focus";
        private string _targetModeName = "Rest";

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

            this._transitionSec = config.TransitionSec;

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
                var state = CalculatePomodoroState(now);

                // 2. 物理演算（数値の補完と揺らぎ）
                var physics = CalculatePhysics(state, delta);

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

        private PomodoroState CalculatePomodoroState(DateTime now)
        {

            // 1. GetCurrentModeName ではなく GetCurrentMode を呼ぶ
            AppConfig.Mode activeMode = _config.GetCurrentMode(now);

            // 2. Auto 以外なら強制/スケジュールモード確定
            if (activeMode != AppConfig.Mode.Auto)
            {
                // Enum を文字列に変換 ("Focus", "Rest" 等)
                string modeKey = activeMode.ToString();

                if (_config.Modes.TryGetValue(modeKey, out var conf))
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

            //// 今「どのモードであるべきか」を判定
            //AppConfig.Mode activeMode = _config.GetCurrentMode(now);

            //// Auto以外（Focus, Rest, Sleepのいずれか）に決定している場合
            //if (activeMode != AppConfig.Mode.Auto)
            //{
            //    string modeKey = activeMode.ToString(); // "Focus", "Rest" など

            //    // Dictionaryに設定が存在するか確認して適用
            //    if (_config.Modes.TryGetValue(modeKey, out var conf))
            //    {
            //        return new PomodoroState
            //        {
            //            ModeName = modeKey,
            //            IsWork = (activeMode == AppConfig.Mode.Focus),
            //            RemainingSec = 0,
            //            ProgressRatio = 1.0,
            //            TransRatio = 0.0,
            //            CurrentSet = conf,
            //            TargetSet = conf
            //        };
            //    }
            //}

            // 1. 強制指定があるかチェック
            //AppConfig.Mode targetMode = _config.OverrideMode;
            //AppConfig.Mode scheduledMode = _config.GetCurrentModeName(now);

            //// 2. 指定がなければスケジュールをチェック
            //if (targetMode != AppConfig.Mode.Auto)
            //{
            //    scheduledMode = targetMode.ToString();
            //}

            //// スケジュールがあれば、ポモドーロ計算を無視してそのモードを強制
            //if (!string.IsNullOrEmpty(scheduledMode) && _config.Modes.ContainsKey(scheduledMode))
            //{
            //    return new PomodoroState
            //    {
            //        ModeName = scheduledMode,
            //        IsWork = false, // スケジュール時は一旦falseで統一（またはモード名で判定）
            //        RemainingSec = 0, // スケジュール中は残り時間なし
            //        ProgressRatio = 1.0,
            //        TransRatio = 0.0, // 常に安定
            //        CurrentSet = _config.Modes[scheduledMode],
            //        TargetSet = _config.Modes[scheduledMode]
            //    };
            //}

            // Dictionaryから安全に設定を取得（キーがない場合の保険も兼ねる）
            var focusConf = _config.Modes.GetValueOrDefault("Focus") ?? new PhaseConfig { Min = 25 };
            var restConf = _config.Modes.GetValueOrDefault("Rest") ?? new PhaseConfig { Min = 5 };

            double focusSec = focusConf.Min * 60.0;
            double restSec = restConf.Min * 60.0;
            double cycle = focusSec + restSec;

            // 現在のサイクル位置
            double currentCycleSec = ((now.Minute * 60) + now.Second + (now.Millisecond / 1000.0)) % cycle;

            bool isFocus = currentCycleSec < focusSec;
            _currentModeName = isFocus ? "Focus" : "Rest";
            _targetModeName = isFocus ? "Rest" : "Focus";

            double rem = isFocus ? focusSec - currentCycleSec : cycle - currentCycleSec;
            double currentPhaseMax = isFocus ? focusSec : restSec;



            return new PomodoroState
            {
                ModeName = _currentModeName, // Stateクラスにプロパティを追加しておくと便利
                IsWork = isFocus, // 互換性のために残す
                RemainingSec = rem,
                ProgressRatio = 1.0 - (rem / currentPhaseMax),
                TransRatio = (rem < _config.TransitionSec) ? (_config.TransitionSec - rem) / _config.TransitionSec : 0.0,
                CurrentSet = _config.Modes.GetValueOrDefault(_currentModeName) ?? new PhaseConfig(),
                TargetSet = _config.Modes.GetValueOrDefault(_targetModeName) ?? new PhaseConfig()
            };
        }



        //private PomodoroState CalculatePomodoroState(DateTime now)
        //{
        //    double workSec = work.Min * 60.0, restSec = rest.Min * 60.0, cycle = workSec + restSec;
        //    double currentCycleSec = ((now.Minute * 60) + now.Second + (now.Millisecond / 1000.0)) % cycle;

        //    bool isWork = currentCycleSec < workSec;
        //    double rem = isWork ? workSec - currentCycleSec : cycle - currentCycleSec;
        //    double currentPhaseMax = isWork ? workSec : restSec;

        //    return new PomodoroState
        //    {
        //        IsWork = isWork,
        //        RemainingSec = rem,
        //        ProgressRatio = 1.0 - (rem / currentPhaseMax),
        //        TransRatio = (rem < _transitionSec) ? (_transitionSec - rem) / _transitionSec : 0.0,
        //        CurrentSet = isWork ? work : rest,
        //        TargetSet = isWork ? rest : work
        //    };
        //}

        private AuroraPhysics CalculatePhysics(PomodoroState state, double delta)
        {
            double cPSec = Lerp(state.CurrentSet.PulseSec, state.TargetSet.PulseSec, state.TransRatio);

            // 2.0 * Math.PI ではなく Math.PI にするか、cPSec を 2倍にする
            // ここでは、1周期で「最小→最大→最小」を完結させる速度を半分に落とします。
            _currentPhase += (Math.PI / cPSec) * delta;
            _currentPhase %= (2.0 * Math.PI);


            // サイン波(-1～1)を0～1に変換
            double oscBlur = (Math.Sin(_currentPhase) + 1.0) / 2.0;


            // --- 2. Opacity用の「違う波」を作る ---
            // 例：少しだけタイミングを遅らせる (+ Math.PI/4)
            // さらに Math.Pow(..., 2) で「パッと明るくなって、じわじわ消える」鋭さを出す
            double opPhase = _currentPhase + (Math.PI * 0.25); // 45度ずらす
            double oscOpacity = (Math.Sin(opPhase) + 1.0) / 2.0;
            oscOpacity = Math.Pow(oscOpacity, 1.5); // 数値を大きくするほど「鋭い」拍動になる

            double bMin = Lerp(state.CurrentSet.BlurMin, state.TargetSet.BlurMin, state.TransRatio);
            double bMax = Lerp(state.CurrentSet.BlurMax, state.TargetSet.BlurMax, state.TransRatio);

            // Flow（流速）の計算
            double flowDur = Lerp(state.CurrentSet.FlowDuration, state.TargetSet.FlowDuration, state.TransRatio);
            _currentFlow = (_currentFlow + (delta / flowDur)) % 1.0;

            // CalculatePhysics 内での計算イメージ
            double pulseCurrentSec = (_currentPhase / (2.0 * Math.PI)) * cPSec;



            double opMin = Lerp(state.CurrentSet.OpMin, state.TargetSet.OpMin, state.TransRatio);
            double opMax = Lerp(state.CurrentSet.OpMax, state.TargetSet.OpMax, state.TransRatio);
            //double currentOpacity = Lerp(opMin, opMax, osc);



            return new AuroraPhysics
            {
                Thick = Lerp(state.CurrentSet.Thick, state.TargetSet.Thick, state.TransRatio),
                Blur = bMin + (bMax - bMin) * oscBlur, // ここで osc (0-1) を使って範囲を補間
                Opacity = opMin + (opMax - opMin) * oscOpacity, // 独自カーブを適用
                Flow = _currentFlow,
                PulseSec = cPSec,
                PulseTime = pulseCurrentSec, // これが「現在の経過秒数」
                Osc = oscBlur
            };
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

                AuroraBrush.GradientStops[i].Color = LerpColorStatic(cSrc, cDst, state.TransRatio);
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
        private double Lerp(double f, double t, double r) => f + (t - f) * Math.Clamp(r, 0, 1);
        public static Color LerpColorStatic(Color c1, Color c2, double r) => Color.FromRgb((byte)(c1.R + (c2.R - c1.R) * r), (byte)(c1.G + (c2.G - c1.G) * r), (byte)(c1.B + (c2.B - c1.B) * r));
    }
}