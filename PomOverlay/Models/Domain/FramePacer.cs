namespace PomOverlay
{
    /// <summary>
    /// 画面のリフレッシュごとに呼ばれる通知を、目標のフレームレートに間引く。
    /// 経過時間を足し込み、1フレーム分たまったら描画する（DispatcherTimer は約15.6ms 刻みに丸められて狙った間隔にならないため）
    /// </summary>
    public class FramePacer
    {
        // リフレッシュ間隔のゆらぎで1回余計に待たないための許容幅(秒)
        private const double Tolerance = 0.004;

        private double _interval;
        private double _accumulated;

        public FramePacer(double fps) => SetFps(fps);

        public void SetFps(double fps) => _interval = 1.0 / fps;

        /// <summary>
        /// 前回の呼び出しからの経過秒数を渡し、今回描画すべきかを返す
        /// </summary>
        public bool Tick(double elapsedSec)
        {
            _accumulated += elapsedSec;
            if (_accumulated + Tolerance < _interval) return false;

            _accumulated -= _interval;
            // スリープ復帰などで大きく遅れた分は取り戻さない
            if (_accumulated > _interval) _accumulated = 0;
            return true;
        }

        public void Reset() => _accumulated = 0;
    }
}
