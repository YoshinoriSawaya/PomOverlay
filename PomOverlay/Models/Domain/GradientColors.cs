using Color = System.Windows.Media.Color;

namespace PomOverlay
{
    public static class GradientColors
    {
        // 現在モードの色から次のモードの色へ、フェード率に応じて補間したグラデーションの色を返す
        public static Color[] Calculate(PomodoroState state, int stopCount)
        {
            var colors = new Color[stopCount];
            for (int i = 0; i < stopCount; i++)
            {
                Color cSrc = state.CurrentSet.GetInterpolatedColor(i, stopCount);
                Color cDst = state.TargetSet.GetInterpolatedColor(i, stopCount);
                colors[i] = Interpolation.LerpColor(cSrc, cDst, state.TransRatio);
            }
            return colors;
        }
    }
}
