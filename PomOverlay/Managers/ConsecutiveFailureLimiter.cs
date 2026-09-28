namespace PomOverlay.Managers
{
    /// <summary>
    /// 同じ処理の連続失敗回数を数え、上限に達したら処理を止めるべきかを判定する
    /// </summary>
    public class ConsecutiveFailureLimiter
    {
        public int Limit { get; }
        public int ConsecutiveFailures { get; private set; }
        public bool IsTripped => ConsecutiveFailures >= Limit;

        public ConsecutiveFailureLimiter(int limit)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
            Limit = limit;
        }

        public void RecordSuccess() => ConsecutiveFailures = 0;

        /// <summary>
        /// 失敗を記録し、ちょうど上限に達したときだけ true を返す
        /// </summary>
        public bool RecordFailure()
        {
            ConsecutiveFailures++;
            return ConsecutiveFailures == Limit;
        }

        public void Reset() => ConsecutiveFailures = 0;
    }
}
