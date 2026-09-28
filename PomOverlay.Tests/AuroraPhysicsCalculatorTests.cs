namespace PomOverlay.Tests
{
    public class AuroraPhysicsCalculatorTests
    {
        private static readonly PhaseConfig Focus = new()
        {
            Thick = 2, BlurMin = 4, BlurMax = 10, PulseSec = 10, FlowDuration = 20, OpMin = 0.2, OpMax = 0.8
        };

        private static readonly PhaseConfig Rest = new()
        {
            Thick = 20, BlurMin = 30, BlurMax = 150, PulseSec = 3, FlowDuration = 10, OpMin = 0.1, OpMax = 0.9
        };

        private static PomodoroState State(PhaseConfig current, PhaseConfig target, double transRatio)
            => new() { CurrentSet = current, TargetSet = target, TransRatio = transRatio };

        [Fact]
        public void Phase_WrapsAround2Pi()
        {
            var calc = new AuroraPhysicsCalculator();
            var state = State(new PhaseConfig { PulseSec = 2, FlowDuration = 1 }, new PhaseConfig { PulseSec = 2, FlowDuration = 1 }, 0);

            // 位相は π/PulseSec 毎秒で進む → 5秒で 2.5π → ラップして 0.5π
            var p = calc.Update(state, 5.0);

            // PulseTime = 位相/2π × PulseSec = 0.25 × 2
            Assert.Equal(0.5, p.PulseTime, 9);
        }

        [Fact]
        public void Phase_StaysWithinOneCycleOverManyFrames()
        {
            var calc = new AuroraPhysicsCalculator();
            var state = State(Focus, Focus, 0);

            for (int i = 0; i < 10_000; i++)
            {
                var p = calc.Update(state, 0.37);

                Assert.InRange(p.PulseTime, 0.0, p.PulseSec);
                Assert.True(p.PulseTime < p.PulseSec);
                Assert.InRange(p.Flow, 0.0, 1.0);
                Assert.True(p.Flow < 1.0);
            }
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(1.0)]
        public void BlurAndOpacity_StayWithinConfiguredRange(double transRatio)
        {
            var calc = new AuroraPhysicsCalculator();
            var state = State(Focus, Rest, transRatio);

            double blurMin = Focus.BlurMin + (Rest.BlurMin - Focus.BlurMin) * transRatio;
            double blurMax = Focus.BlurMax + (Rest.BlurMax - Focus.BlurMax) * transRatio;
            double opMin = Focus.OpMin + (Rest.OpMin - Focus.OpMin) * transRatio;
            double opMax = Focus.OpMax + (Rest.OpMax - Focus.OpMax) * transRatio;

            for (int i = 0; i < 2_000; i++)
            {
                var p = calc.Update(state, 0.016);

                Assert.InRange(p.Blur, blurMin - 1e-9, blurMax + 1e-9);
                Assert.InRange(p.Opacity, opMin - 1e-9, opMax + 1e-9);
                Assert.InRange(p.Osc, 0.0, 1.0);
            }
        }

        [Theory]
        [InlineData(-0.5)]
        [InlineData(1.5)]
        public void TransRatioOutsideZeroToOne_IsClamped(double transRatio)
        {
            var calc = new AuroraPhysicsCalculator();

            var p = calc.Update(State(Focus, Rest, transRatio), 0.016);

            double expected = transRatio < 0 ? Focus.Thick : Rest.Thick;
            Assert.Equal(expected, p.Thick, 9);
        }

        [Fact]
        public void Thick_IsInterpolatedByTransRatio()
        {
            var calc = new AuroraPhysicsCalculator();

            var p = calc.Update(State(Focus, Rest, 0.25), 0.016);

            Assert.Equal(2 + (20 - 2) * 0.25, p.Thick, 9);
        }
    }
}
