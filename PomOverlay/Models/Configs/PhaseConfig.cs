using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace PomOverlay
{
    public class PhaseConfig
    {
        //public string _Help { get; set; } = "Min:分, ColorStrings:色の配列, Thick:枠太さ, BlurMin-Max:ぼかし範囲, PulseSec:明滅周期(秒), FlowDuration:流速(秒)";

        public int Min { get; set; }
        public string[] ColorStrings { get; set; } = Array.Empty<string>();
        public double Thick { get; set; }
        public double BlurMin { get; set; }
        public double BlurMax { get; set; }
        public double PulseSec { get; set; }
        public double FlowDuration { get; set; }

        public double OpMin { get; set; } = 0.2; // 最小不透明度
        public double OpMax { get; set; } = 0.8; // 最大不透明度

        public Color GetInterpolatedColor(int index, int totalStops)
        {
            // ここで null チェックを入れるとより安全です
            if (ColorStrings == null || ColorStrings.Length == 0) return Colors.Black;
            if (ColorStrings.Length == 1) return (Color)ColorConverter.ConvertFromString(ColorStrings[0]);

            double ratio = (double)index / (totalStops - 1);
            double floatIndex = ratio * (ColorStrings.Length - 1);
            int i1 = (int)Math.Floor(floatIndex);
            int i2 = (int)Math.Ceiling(floatIndex);
            double localRatio = floatIndex - i1;

            Color c1 = (Color)ColorConverter.ConvertFromString(ColorStrings[i1]);
            Color c2 = (Color)ColorConverter.ConvertFromString(ColorStrings[i2]);

            return MainWindow.LerpColorStatic(c1, c2, localRatio);
        }
    }

}