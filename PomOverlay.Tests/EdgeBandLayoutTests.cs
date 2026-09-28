namespace PomOverlay.Tests
{
    public class EdgeBandLayoutTests
    {
        private static readonly PhaseConfig Focus = new() { Thick = 2, BlurMin = 4, BlurMax = 10 };
        private static readonly PhaseConfig Rest = new() { Thick = 50, BlurMin = 30, BlurMax = 50 };

        private static PomodoroState State(PhaseConfig current, PhaseConfig target, double remainingSec, double transRatio = 0)
            => new() { CurrentSet = current, TargetSet = target, RemainingSec = remainingSec, TransRatio = transRatio };

        [Fact]
        public void BandWidth_FarFromTransition_UsesCurrentModeOnly()
        {
            var width = EdgeBandLayout.CalculateBandWidth(State(Focus, Rest, remainingSec: 600), transitionSec: 30);

            Assert.Equal(12 + EdgeBandLayout.Padding, width);
        }

        [Theory]
        [InlineData(32.0, 0.0)]  // フェード開始の猶予(2秒)に入った
        [InlineData(15.0, 0.5)]  // フェード中
        public void BandWidth_NearOrDuringTransition_CoversTargetMode(double remainingSec, double transRatio)
        {
            var width = EdgeBandLayout.CalculateBandWidth(State(Focus, Rest, remainingSec, transRatio), transitionSec: 30);

            Assert.Equal(100 + EdgeBandLayout.Padding, width);
        }

        [Fact]
        public void BandWidth_JustBeforeLead_StillCurrentOnly()
        {
            var width = EdgeBandLayout.CalculateBandWidth(State(Focus, Rest, remainingSec: 32.5), transitionSec: 30);

            Assert.Equal(12 + EdgeBandLayout.Padding, width);
        }

        [Fact]
        public void BandWidth_ForcedMode_UsesThatMode()
        {
            // 強制・スケジュール時は Current と Target が同じで RemainingSec = 0
            var width = EdgeBandLayout.CalculateBandWidth(State(Rest, Rest, remainingSec: 0), transitionSec: 30);

            Assert.Equal(100 + EdgeBandLayout.Padding, width);
        }

        [Fact]
        public void BandWidth_UsesLargerOfBlurMinAndMax_AndRoundsUp()
        {
            var odd = new PhaseConfig { Thick = 2.2, BlurMin = 10, BlurMax = 3 };

            var width = EdgeBandLayout.CalculateBandWidth(State(odd, odd, remainingSec: 600), transitionSec: 30);

            Assert.Equal(13 + EdgeBandLayout.Padding, width);
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
