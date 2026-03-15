using System;

namespace PomOverlay
{
    public class PomodoroState
    {
        // 1. 文字列ベースのモード名を持つようにする
        public string ModeName { get; set; } = "Focus";

        // 既存のプロパティ（互換性のために IsWork も残す）
        public bool IsWork { get; set; }
        public double RemainingSec { get; set; }
        public double ProgressRatio { get; set; }
        public double TransRatio { get; set; }
        public PhaseConfig CurrentSet { get; set; } = null!;
        public PhaseConfig TargetSet { get; set; } = null!;

        // 2. 表示名を Dictionary のキーに基づいて柔軟に返す
        public string GetDisplayName(bool isJapanese)
        {
            return ModeName switch
            {
                "Focus" => isJapanese ? "集中モード 🎯" : "FOCUS",
                "Rest" => isJapanese ? "休憩モード ☕" : "REST",
                "Sleep" => isJapanese ? "睡眠モード 🌙" : "SLEEP",
                "Lunch" => isJapanese ? "昼食休憩 🍴" : "LUNCH",
                _ => ModeName.ToUpper() // 未定義の場合は大文字で表示
            };
        }

        // 3. フェード状態の文字列表示（ロジックは維持し、ラベルを少し整理）
        public string GetTransitionText(bool isJapanese, double transitionSec)
        {
            string label = isJapanese ? "切替　　" : "TRANS   ";
            string secUnit = isJapanese ? "秒" : "s ";

            if (TransRatio <= 0)
            {
                string status = isJapanese ? "待機中..." : "Steady...";
                return $"{label}: {0.0,5:0.0} %   / {transitionSec,6:0.0}{secUnit} {status}";
            }
            else
            {
                string status = isJapanese ? "切替中..." : "Fading...";
                return $"{label}: {TransRatio * 100,5:0.0} %   / {transitionSec,6:0.0}{secUnit} {status}";
            }
        }
    }
}