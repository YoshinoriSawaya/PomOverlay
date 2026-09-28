using PomOverlay.Managers;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;

namespace PomOverlay
{
    public partial class MainWindow : Window
    {
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
        private readonly DispatcherTimer _timer;
        private readonly ConsecutiveFailureLimiter _updateFailures = new(10);

        private readonly DebugLabels _labels = new();

        public MainWindow(Rect bounds, int index, AppConfig config)
        {
            InitializeComponent();

            this.ScreenIndex = index;
            this._config = config;
            this.Left = bounds.X; this.Top = bounds.Y; this.Width = bounds.Width; this.Height = bounds.Height;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };//30FPS:33  60なら16に
            _timer.Tick += Update;
            _timer.Start();
        }

        public void UpdateConfig(AppConfig config)
        {
            this._config = config;
            _labels.SetLanguage(_isJapanese);

            // 連続失敗で止まっていた場合、設定の変更（リロード等）を機に再開する
            _updateFailures.Reset();
            if (!_timer.IsEnabled)
            {
                _lastTick = DateTime.Now;
                _timer.Start();
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }

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

                _updateFailures.RecordSuccess();
            }
            catch (Exception ex)
            {
                // 16msごとに同じ例外でログを吐き続けないよう、連続失敗が上限に達したらループを止める
                if (_updateFailures.RecordFailure())
                {
                    _timer.Stop();
                    _debugManager.LogError($"Update Loop Error ({_updateFailures.Limit}回連続で失敗したため停止。設定のリロードで再開)", ex);
                }
                else
                {
                    _debugManager.LogError("Update Loop Error", ex);
                }
            }
        }

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
                Color cSrc = state.CurrentSet.GetInterpolatedColor(i, stopCount);
                Color cDst = state.TargetSet.GetInterpolatedColor(i, stopCount);

                AuroraBrush.GradientStops[i].Color = Interpolation.LerpColor(cSrc, cDst, state.TransRatio);
            }
        }

        private void UpdateDebugText(DateTime now, PomodoroState s, AuroraPhysics p, double delta)
        {
            DebugText.Text = _debugManager.GenerateDebugText(
                now, s, p, _config, ScreenIndex, _labels, delta);
        }

        public void SetLanguage(bool jp)
        {
            _isJapanese = jp;
            _labels.SetLanguage(_isJapanese);
            _debugManager.SetLanguage(jp);
        }

        public void SetDebugVisibility(bool v) => DebugContainer.Visibility = v ? Visibility.Visible : Visibility.Collapsed;
    }
}
