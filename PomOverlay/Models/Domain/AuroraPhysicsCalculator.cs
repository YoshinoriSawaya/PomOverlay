namespace PomOverlay
{
    // ポモドーロの状態と経過時間から、オーロラの物理パラメータ（揺らぎ・流れ）を進める
    public class AuroraPhysicsCalculator
    {
        private double _currentPhase = 0, _currentFlow = 0;

        public AuroraPhysics Update(PomodoroState state, double delta)
        {
            double cPSec = Interpolation.Lerp(state.CurrentSet.PulseSec, state.TargetSet.PulseSec, state.TransRatio);

            // 1周期で「最小→最大→最小」を完結させるため、2π ではなく π で進める
            _currentPhase += (Math.PI / cPSec) * delta;
            _currentPhase %= (2.0 * Math.PI);

            // サイン波(-1～1)を0～1に変換
            double oscBlur = (Math.Sin(_currentPhase) + 1.0) / 2.0;

            // Opacity用に45度ずらした波を作り、Math.Pow で「パッと明るくなって、じわじわ消える」鋭さを出す
            double opPhase = _currentPhase + (Math.PI * 0.25);
            double oscOpacity = (Math.Sin(opPhase) + 1.0) / 2.0;
            oscOpacity = Math.Pow(oscOpacity, 1.5); // 数値を大きくするほど「鋭い」拍動になる

            double bMin = Interpolation.Lerp(state.CurrentSet.BlurMin, state.TargetSet.BlurMin, state.TransRatio);
            double bMax = Interpolation.Lerp(state.CurrentSet.BlurMax, state.TargetSet.BlurMax, state.TransRatio);

            // Flow（流速）の計算
            double flowDur = Interpolation.Lerp(state.CurrentSet.FlowDuration, state.TargetSet.FlowDuration, state.TransRatio);
            _currentFlow = (_currentFlow + (delta / flowDur)) % 1.0;

            // 拍動の現在の経過秒数
            double pulseCurrentSec = (_currentPhase / (2.0 * Math.PI)) * cPSec;

            double opMin = Interpolation.Lerp(state.CurrentSet.OpMin, state.TargetSet.OpMin, state.TransRatio);
            double opMax = Interpolation.Lerp(state.CurrentSet.OpMax, state.TargetSet.OpMax, state.TransRatio);

            return new AuroraPhysics
            {
                Thick = Interpolation.Lerp(state.CurrentSet.Thick, state.TargetSet.Thick, state.TransRatio),
                Blur = bMin + (bMax - bMin) * oscBlur,
                Opacity = opMin + (opMax - opMin) * oscOpacity,
                Flow = _currentFlow,
                PulseSec = cPSec,
                PulseTime = pulseCurrentSec,
                Osc = oscBlur
            };
        }
    }
}
