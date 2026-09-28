using Color = System.Windows.Media.Color;

namespace PomOverlay
{
    public static class Interpolation
    {
        public static double Lerp(double f, double t, double r) => f + (t - f) * Math.Clamp(r, 0, 1);

        public static Color LerpColor(Color c1, Color c2, double r) => Color.FromRgb((byte)(c1.R + (c2.R - c1.R) * r), (byte)(c1.G + (c2.G - c1.G) * r), (byte)(c1.B + (c2.B - c1.B) * r));
    }
}
