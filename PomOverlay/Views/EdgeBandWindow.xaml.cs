using System;
using System.Windows;
using System.Windows.Controls;
using Color = System.Windows.Media.Color;

namespace PomOverlay
{
    // モニターの縁の帯1本分のウィンドウ。描画内容は MonitorOverlay から渡される
    public partial class EdgeBandWindow : Window
    {
        public const int GradientStopCount = 7;

        public EdgeBandWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// monitor はモニター全体の矩形、band はその中でこのウィンドウが受け持つ帯（モニター左上基準）
        /// </summary>
        public void Place(Rect monitor, BandRect band)
        {
            Left = monitor.X + band.X; Top = monitor.Y + band.Y; Width = band.Width; Height = band.Height;

            AuroraRect.Width = monitor.Width;
            AuroraRect.Height = monitor.Height;
            Canvas.SetLeft(AuroraRect, -band.X);
            Canvas.SetTop(AuroraRect, -band.Y);
        }

        public void Apply(AuroraPhysics physics, Color[] stopColors)
        {
            AuroraRect.StrokeThickness = physics.Thick;
            BlurEff.Radius = physics.Blur;
            AuroraRect.Opacity = physics.Opacity;

            BrushTransform.X = physics.Flow;
            BrushTransform.Y = physics.Flow;

            for (int i = 0; i < stopColors.Length; i++)
            {
                AuroraBrush.GradientStops[i].Color = stopColors[i];
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ClickThrough.Apply(this);
        }
    }
}
