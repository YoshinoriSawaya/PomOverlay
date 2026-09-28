namespace PomOverlay.Tests
{
    public class FramePacerTests
    {
        private static int CountFrames(FramePacer pacer, double refreshHz, double seconds, double jitter = 0)
        {
            var rng = new Random(1);
            int frames = 0;
            int ticks = (int)(refreshHz * seconds);
            for (int i = 0; i < ticks; i++)
            {
                double dt = 1.0 / refreshHz + (rng.NextDouble() - 0.5) * jitter;
                if (pacer.Tick(dt)) frames++;
            }
            return frames;
        }

        [Theory]
        [InlineData(30, 60)]
        [InlineData(30, 144)]
        [InlineData(20, 60)]
        [InlineData(60, 60)]
        [InlineData(30, 120)]
        public void Tick_ProducesTargetFrameRate(double fps, double refreshHz)
        {
            int frames = CountFrames(new FramePacer(fps), refreshHz, 10);

            Assert.InRange(frames / 10.0, fps * 0.9, fps * 1.01);
        }

        [Fact]
        public void Tick_ToleratesRefreshJitter()
        {
            // 60Hz で ±2ms ゆらいでも、30FPS がほぼ保たれる
            int frames = CountFrames(new FramePacer(30), 60, 10, jitter: 0.004);

            Assert.InRange(frames / 10.0, 29, 30.3);
        }

        [Fact]
        public void Tick_AtEveryOtherRefresh_For30FpsOn60Hz()
        {
            var pacer = new FramePacer(30);

            var pattern = Enumerable.Range(0, 6).Select(_ => pacer.Tick(1.0 / 60)).ToArray();

            Assert.Equal([false, true, false, true, false, true], pattern);
        }

        [Fact]
        public void Tick_DoesNotCatchUpAfterLongPause()
        {
            var pacer = new FramePacer(30);

            Assert.True(pacer.Tick(5.0));      // スリープ復帰などで5秒空いた
            Assert.False(pacer.Tick(1.0 / 60)); // 取り戻すための連続描画はしない
            Assert.True(pacer.Tick(1.0 / 60));
        }

        [Fact]
        public void SetFps_ChangesInterval()
        {
            var pacer = new FramePacer(30);
            pacer.SetFps(60);

            Assert.Equal(60, CountFrames(pacer, 60, 1));
        }
    }
}
