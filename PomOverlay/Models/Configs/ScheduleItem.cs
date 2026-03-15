using System.Collections.Generic;
using static PomOverlay.AppConfig;

namespace PomOverlay
{
    // --- JSON用のスケジュールクラスも修正 ---
    public class ScheduleItem
    {
        public string Start { get; set; } = "00:00";
        public string End { get; set; } = "00:00";

        // 文字列で保存されますが、enumとして扱えるようになります
        public Mode ApplyMode { get; set; } = Mode.Rest;
    }
}