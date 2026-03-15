using System.Collections.Generic;
using System.Text;

namespace PomOverlay
{

    /// <summary>
    /// アプリ全体の基本設定
    /// </summary>
    /* * [各設定項目の解説]
     * TransitionSec : モードが切り替わる際のフェード時間（秒）。
     * Modes         : モード名（Focus, Rest, Sleep等）をキーとした設定の辞書。
     * Schedules     : 特定の時間帯に強制的にモードを固定するスケジュールのリスト。
     */
    public class AppConfig
    {
        // AppConfig.cs または管理クラスに追記
        public enum Mode { Auto, Focus, Rest, Sleep }
        public Mode OverrideMode { get; set; } = Mode.Auto;

        public List<ScheduleItem> Schedules { get; set; } = new();
        public Dictionary<string, PhaseConfig> Modes { get; set; } = new();

        // フェードにかける時間
        public double TransitionSec { get; set; } = 30.0;

        // モード設定（Dictionary形式で動的に増やせる）
        //public Dictionary<Mode, PhaseConfig> Modes { get; set; } = new();

        // 強制割り込みスケジュール
        //public List<ScheduleItem> Schedules { get; set; } = new();

        public PhaseConfig GetModeConfig(Mode modeName)
        {
            // 指定された名前があればそれを、なければ最初のモードを、それもなければ空の設定を返す
            if (Modes.TryGetValue(modeName.ToString(), out var config)) return config;

            // フォールバック（保険）
            foreach (var first in Modes.Values) return first;
            return new PhaseConfig();
        }


        // 判定メソッドの修正
        public Mode GetCurrentMode(DateTime now)
        {
            // 1. 強制設定があればそれを最優先で返す
            if (OverrideMode != Mode.Auto) return OverrideMode;

            // 2. スケジュールを確認
            foreach (var schedule in Schedules)
            {
                if (TimeSpan.TryParse(schedule.Start, out var start) &&
                    TimeSpan.TryParse(schedule.End, out var end))
                {
                    var currentTime = now.TimeOfDay;
                    bool isInRange = (start <= end)
                        ? (currentTime >= start && currentTime < end)
                        : (currentTime >= start || currentTime < end);

                    if (isInRange) return schedule.ApplyMode;
                }
            }

            // 3. 何もなければ Auto (ポモドーロサイクルへ)
            return Mode.Auto;
        }
        public string GetScheduleSummary()
        {
            var sb = new StringBuilder();
            if (Schedules.Count == 0) return "No Schedules Defined";

            foreach (var s in Schedules)
            {
                // [00:00-06:00] -> SLEEP
                sb.AppendLine($" [{s.Start}-{s.End}] -> {s.ApplyMode.ToString().ToUpper()}");
            }
            return sb.ToString().TrimEnd();
        }

        public static AppConfig CreateDefault()
        {
            var config = new AppConfig
            {
                TransitionSec = 30.0
            };

            // Focusモード
            config.Modes["Focus"] = new PhaseConfig
            {
                Min = 25,
                Thick = 2,
                BlurMin = 4,
                BlurMax = 10,
                PulseSec = 10,
                FlowDuration = 20,
                ColorStrings = ["DeepSkyBlue", "Cyan", "Aquamarine", "RoyalBlue", "DeepSkyBlue"]
            };

            // Restモード
            config.Modes["Rest"] = new PhaseConfig
            {
                Min = 5,
                Thick = 20,
                BlurMin = 30,
                BlurMax = 150, // 休憩は大胆にぼかす
                PulseSec = 3,
                FlowDuration = 10,
                ColorStrings = ["DarkRed", "Red", "DarkRed", "OrangeRed", "DarkOrange"]
            };

            // Sleepモード (追加！)
            config.Modes["Sleep"] = new PhaseConfig
            {
                Min = 0, // タイマー用ではないので0
                Thick = 1,
                BlurMin = 100,
                BlurMax = 300,
                PulseSec = 30, // 非常にゆっくり
                FlowDuration = 120,
                ColorStrings = ["#000011", "#000033", "#050022"] // 深夜の深い紺色
            };

            // 初期スケジュール例
            config.Schedules.Add(new ScheduleItem { Start = "00:00", End = "06:00", ApplyMode = Mode.Sleep });
            config.Schedules.Add(new ScheduleItem { Start = "12:00", End = "13:00", ApplyMode = Mode.Rest });

            return config;
        }

    }


}