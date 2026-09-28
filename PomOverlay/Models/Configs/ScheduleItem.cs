using System.Collections.Generic;
using static PomOverlay.AppConfig;

namespace PomOverlay
{
    // config.json の Schedules の1要素
    public class ScheduleItem
    {
        public string Start { get; set; } = "00:00";
        public string End { get; set; } = "00:00";

        // config.json では "Sleep" のような名前で書く(古い数値形式も読める)
        public Mode ApplyMode { get; set; } = Mode.Rest;
    }
}