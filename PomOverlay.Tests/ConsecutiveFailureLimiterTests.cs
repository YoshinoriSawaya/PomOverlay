using PomOverlay.Managers;

namespace PomOverlay.Tests
{
    public class ConsecutiveFailureLimiterTests
    {
        [Fact]
        public void RecordFailure_ReturnsTrueOnlyWhenReachingLimit()
        {
            var limiter = new ConsecutiveFailureLimiter(3);

            Assert.False(limiter.RecordFailure());
            Assert.False(limiter.RecordFailure());
            Assert.True(limiter.RecordFailure());
            Assert.True(limiter.IsTripped);

            // 上限を超えた後は「今止める」合図を繰り返さない
            Assert.False(limiter.RecordFailure());
            Assert.True(limiter.IsTripped);
        }

        [Fact]
        public void RecordSuccess_ResetsConsecutiveCount()
        {
            var limiter = new ConsecutiveFailureLimiter(3);

            limiter.RecordFailure();
            limiter.RecordFailure();
            limiter.RecordSuccess();

            Assert.Equal(0, limiter.ConsecutiveFailures);
            Assert.False(limiter.RecordFailure());
            Assert.False(limiter.RecordFailure());
            Assert.True(limiter.RecordFailure());
        }

        [Fact]
        public void Reset_ClearsTrippedState()
        {
            var limiter = new ConsecutiveFailureLimiter(1);

            Assert.True(limiter.RecordFailure());
            limiter.Reset();

            Assert.False(limiter.IsTripped);
            Assert.True(limiter.RecordFailure());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_RejectsNonPositiveLimit(int limit)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConsecutiveFailureLimiter(limit));
        }
    }
}
