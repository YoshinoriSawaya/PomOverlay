using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace PomOverlay
{
    public class DebugLabels
    {
        public string Header { get; private set; } = "";
        public string Mode { get; private set; } = "";
        public string Time { get; private set; } = "";
        public string Progress { get; private set; } = "";

        // 項目名
        public string ThickTitle { get; private set; } = "";
        public string BlurTitle { get; private set; } = "";
        public string OpTitle { get; private set; } = "";
        public string PulseTitle { get; private set; } = "";
        public string FlowTitle { get; private set; } = "";

        // 補足ラベル
        public string SettingLabel { get; private set; } = "";
        public string RangeLabel { get; private set; } = "";
        public string CycleLabel { get; private set; } = "";

        public string Osc { get; private set; } = "";

        public void SetLanguage(bool isJapanese)
        {
            Header = isJapanese ? "画面　" : "SCREEN";
            Mode = isJapanese ? "状態    " : "MODE    "; // 半角8文字分に合わせる
            Time = isJapanese ? "時間    " : "TIME    ";
            Progress = isJapanese ? "進捗    " : "PROG    ";

            // 全角4文字 ＝ 半角8文字
            ThickTitle = isJapanese ? "太さ    " : "THICK   ";
            BlurTitle = isJapanese ? "ぼかし  " : "BLUR    "; // ぼかし(3文字)+全角スペース1
            PulseTitle = isJapanese ? "拍動　　" : "PULSE   ";
            OpTitle = isJapanese ? "不透明度" : "Opacity ";
            FlowTitle = isJapanese ? "流動　　" : "FLOW    ";

            SettingLabel = isJapanese ? "設定　" : "SET   ";
            RangeLabel = isJapanese ? "範囲　" : "RANGE ";
            CycleLabel = isJapanese ? "周期　" : "CYCLE ";

            Osc = isJapanese ? "振幅　  " : "OSC     ";
        }
    }


}