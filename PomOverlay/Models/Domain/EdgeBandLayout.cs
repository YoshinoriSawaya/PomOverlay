namespace PomOverlay
{
    // モニター左上を原点とした帯の矩形
    public readonly record struct BandRect(double X, double Y, double Width, double Height);

    /// <summary>
    /// オーロラが描かれる「画面の縁の帯」の幅と配置を計算する
    /// </summary>
    public static class EdgeBandLayout
    {
        // ぼかしの裾や丸め誤差のための余白
        public const double Padding = 2;

        // フェードが始まる少し前から帯を広げておくための猶予(秒)
        public const double TransitionLeadSec = 2;

        /// <summary>
        /// 今のモードの「線の太さ＋ぼかし半径の最大」から帯幅を決める。
        /// フェードが近い（残り TransitionSec＋猶予 以内）ときだけフェード先のモードも含める。
        /// フェード中は Thick と Blur が同じ比率で補間されるため、合計が2つのモードの大きい方を超えることはない
        /// </summary>
        public static double CalculateBandWidth(PomodoroState state, double transitionSec)
        {
            double width = Extent(state.CurrentSet);
            if (state.TransRatio > 0 || state.RemainingSec <= transitionSec + TransitionLeadSec)
            {
                width = Math.Max(width, Extent(state.TargetSet));
            }
            return Math.Ceiling(width) + Padding;
        }

        private static double Extent(PhaseConfig mode) => mode.Thick + Math.Max(mode.BlurMin, mode.BlurMax);

        /// <summary>
        /// 上・下・左・右の帯を返す（左右は上下の帯と重ならない高さ）。
        /// 帯が画面の半分以上を占める場合は、画面全体を1つの帯として返す
        /// </summary>
        public static IReadOnlyList<BandRect> Calculate(double width, double height, double band)
        {
            if (band * 2 >= Math.Min(width, height))
            {
                return [new BandRect(0, 0, width, height)];
            }

            return
            [
                new BandRect(0, 0, width, band),
                new BandRect(0, height - band, width, band),
                new BandRect(0, band, band, height - band * 2),
                new BandRect(width - band, band, band, height - band * 2),
            ];
        }
    }
}
