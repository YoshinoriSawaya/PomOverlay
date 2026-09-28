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

        /// <summary>
        /// 線の太さ＋ぼかし半径の最大値から帯幅を決める。
        /// フェード中は Thick と Blur が同じ比率で補間されるため、合計がモードごとの最大値を超えることはない
        /// </summary>
        public static double CalculateBandWidth(AppConfig config)
        {
            double max = 0;
            foreach (var mode in config.Modes.Values)
            {
                max = Math.Max(max, mode.Thick + Math.Max(mode.BlurMin, mode.BlurMax));
            }
            return Math.Ceiling(max) + Padding;
        }

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
