namespace PomOverlay.Tests
{
    public class EdgeBandLayoutTests
    {
        [Fact]
        public void BandWidth_UsesMaxOfThickPlusBlurAcrossModes()
        {
            var config = new AppConfig();
            config.Modes["Focus"] = new PhaseConfig { Thick = 2, BlurMax = 10 };
            config.Modes["Rest"] = new PhaseConfig { Thick = 20, BlurMax = 150 };
            config.Modes["Sleep"] = new PhaseConfig { Thick = 1, BlurMax = 100.4 };

            Assert.Equal(170 + EdgeBandLayout.Padding, EdgeBandLayout.CalculateBandWidth(config));
        }

        [Fact]
        public void BandWidth_RoundsUp()
        {
            var config = new AppConfig();
            config.Modes["Focus"] = new PhaseConfig { Thick = 2.2, BlurMax = 10 };

            Assert.Equal(13 + EdgeBandLayout.Padding, EdgeBandLayout.CalculateBandWidth(config));
        }

        [Fact]
        public void BandWidth_NoModes_IsPaddingOnly()
        {
            Assert.Equal(EdgeBandLayout.Padding, EdgeBandLayout.CalculateBandWidth(new AppConfig()));
        }

        [Fact]
        public void Calculate_ReturnsFourNonOverlappingBandsCoveringTheEdges()
        {
            var bands = EdgeBandLayout.Calculate(1920, 1080, 100);

            Assert.Equal(
            [
                new BandRect(0, 0, 1920, 100),
                new BandRect(0, 980, 1920, 100),
                new BandRect(0, 100, 100, 880),
                new BandRect(1820, 100, 100, 880),
            ], bands);

            // 帯の面積の合計 = 画面全体 − 内側の矩形
            double area = bands.Sum(b => b.Width * b.Height);
            Assert.Equal(1920.0 * 1080 - 1720.0 * 880, area);
        }

        [Theory]
        [InlineData(540)]
        [InlineData(1000)]
        public void Calculate_BandTooWide_FallsBackToFullScreen(double band)
        {
            var bands = EdgeBandLayout.Calculate(1920, 1080, band);

            Assert.Equal([new BandRect(0, 0, 1920, 1080)], bands);
        }
    }
}
