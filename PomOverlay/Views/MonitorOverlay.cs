using PomOverlay.Managers;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

namespace PomOverlay
{
    /// <summary>
    /// 1モニター分のオーバーレイ。縁の帯ウィンドウ群とデバッグウィンドウを持ち、
    /// タイマーでロジッククラスを呼び出して結果を各ウィンドウに反映する
    /// </summary>
    public class MonitorOverlay
    {
        private readonly DebugManager _debugManager = new();
        private readonly Rect _bounds;
        private readonly List<EdgeBandWindow> _bands = new();
        private readonly DebugWindow _debugWindow = new();

        public int ScreenIndex { get; }
        public bool IsDebugVisible => _debugWindow.IsVisible;

        private bool _isJapanese = true;
        private DateTime _lastTick = DateTime.Now;
        private double _bandWidth = -1;

        private AppConfig _config;
        private readonly AuroraPhysicsCalculator _physics = new();
        private readonly DispatcherTimer _timer;
        private readonly ConsecutiveFailureLimiter _updateFailures = new(10);

        private readonly DebugLabels _labels = new();

        public MonitorOverlay(Rect bounds, int index, AppConfig config)
        {
            ScreenIndex = index;
            _bounds = bounds;
            _config = config;

            _debugWindow.Left = bounds.X + 20;
            _debugWindow.Top = bounds.Y + 20;

            LayoutBands();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };//30FPS:33  60なら16に
            _timer.Tick += Update;
        }

        public void Show()
        {
            _bands.ForEach(b => b.Show());
            _timer.Start();
        }

        public void UpdateConfig(AppConfig config)
        {
            _config = config;
            _labels.SetLanguage(_isJapanese);
            LayoutBands();

            // 連続失敗で止まっていた場合、設定の変更（リロード等）を機に再開する
            _updateFailures.Reset();
            if (!_timer.IsEnabled)
            {
                _lastTick = DateTime.Now;
                _timer.Start();
            }
        }

        // 帯幅は設定からだけ決まるので、設定が変わったときだけ配置し直す
        private void LayoutBands()
        {
            double bandWidth = EdgeBandLayout.CalculateBandWidth(_config);
            if (bandWidth == _bandWidth) return;
            _bandWidth = bandWidth;

            var rects = EdgeBandLayout.Calculate(_bounds.Width, _bounds.Height, bandWidth);
            bool wasShown = _bands.Count > 0 && _bands[0].IsVisible;

            if (rects.Count != _bands.Count)
            {
                _bands.ForEach(b => b.Close());
                _bands.Clear();
                foreach (var _ in rects) _bands.Add(new EdgeBandWindow());
            }

            for (int i = 0; i < rects.Count; i++)
            {
                _bands[i].Place(_bounds, rects[i]);
                if (wasShown) _bands[i].Show();
            }
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

                // 3. 描画反映（各帯ウィンドウへの適用）
                var colors = GradientColors.Calculate(state, EdgeBandWindow.GradientStopCount);
                foreach (var band in _bands) band.Apply(physics, colors);

                // 4. デバッグ表示
                if (IsDebugVisible)
                {
                    _debugWindow.SetText(_debugManager.GenerateDebugText(
                        now, state, physics, _config, ScreenIndex, _labels, delta));
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

        public void SetLanguage(bool jp)
        {
            _isJapanese = jp;
            _labels.SetLanguage(_isJapanese);
            _debugManager.SetLanguage(jp);
        }

        public void SetDebugVisibility(bool v)
        {
            if (v) _debugWindow.Show();
            else _debugWindow.Hide();
        }
    }
}
