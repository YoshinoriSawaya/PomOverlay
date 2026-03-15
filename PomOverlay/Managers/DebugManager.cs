using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO; // 追加

namespace PomOverlay.Managers
{
    public class DebugManager
    {
        private bool _isJapanese = true;
        private readonly string[] _spinnerFrames = { "  ", "  ", ". ", "..", ".." };
        private readonly string _logPath = "error.log";

        private double _lastFpsDisplayValue = 0;
        private DateTime _lastFpsUpdateTime = DateTime.MinValue;

        // 直近30フレーム分の delta を覚えておく
        private readonly Queue<double> _deltaHistory = new();
        private const int MaxHistory = 30;

        public void SetLanguage(bool jp) => _isJapanese = jp;

        /// <summary>
        /// デバッグウィンドウに表示する全テキストを構築します
        /// </summary>
        public string GenerateDebugText(
            DateTime now,
            PomodoroState s,
            AuroraPhysics p,
            AppConfig config,
            int screenIndex,
            DebugLabels labels,
            double delta)
        {
            const int TotalWidth = 44;
            string line = new string('-', TotalWidth);
            var sb = new StringBuilder();

            // 1. ヘッダー情報
            sb.AppendLine($"[ {labels.Header}: {screenIndex} ] {now:HH:mm:ss}");
            // 1. 履歴を更新
            _deltaHistory.Enqueue(delta);
            if (_deltaHistory.Count > MaxHistory) _deltaHistory.Dequeue();

            // 2. 平均 delta を計算（0除算防止）
            double avgDelta = _deltaHistory.Count > 0 ? _deltaHistory.Average() : 0;

            // 3. 平均 FPS を計算
            double fps = avgDelta > 0 ? 1.0 / avgDelta : 0;

            // 4. 表示（0.5秒ごとの更新と組み合わせると最強に読みやすいです）
            if ((now - _lastFpsUpdateTime).TotalMilliseconds > 1000)
            {
                _lastFpsDisplayValue = fps;
                _lastFpsUpdateTime = now;
            }

            sb.AppendLine($"Performance: {_lastFpsDisplayValue,5:0.0} FPS (avg)");
            //// FPS表示の更新頻度を抑える（0.5秒ごと）
            //if ((now - _lastFpsUpdateTime).TotalMilliseconds > 1000)
            //{
            //    _lastFpsDisplayValue = delta > 0 ? 1.0 / delta : 0;
            //    _lastFpsUpdateTime = now;
            //}
            //// 小数点第1位までに固定し、桁数を揃えて表示
            //sb.AppendLine($"Performance: {(delta > 0 ? 1.0 / delta : 0),5:0.0} FPS");
            sb.AppendLine(line);
            //// 簡易FPS計算のヒント
            //double fps = 1.0 / delta;
            //sb.AppendLine($"Performance: {fps,5:0.0} FPS");
            //sb.AppendLine(line);

            // 2. モード情報
            var scheduledMode = config.GetCurrentMode(now);
            string modeString = scheduledMode.ToString().ToUpper();
            bool isForced = scheduledMode != AppConfig.Mode.Auto;

            string modeType = _isJapanese
                ? (isForced ? "【スケジュール動作中】" : "【自動】")
                : (isForced ? "[SCHEDULED]" : "[AUTO]");

            sb.AppendLine($"{labels.Mode}: {s.GetDisplayName(_isJapanese)} {modeType}");
            sb.AppendLine($"{labels.Time}: {Math.Floor(s.RemainingSec / 60):00}:{Math.Floor(s.RemainingSec % 60):00}");

            // 3. プログレスバー (スピナー付き)
            int barWidth = 24;
            double prog = Math.Clamp(s.ProgressRatio, 0, 1);
            int filled = (int)(prog * barWidth);
            int spinnerIndex = (now.Millisecond / (1000 / _spinnerFrames.Length)) % _spinnerFrames.Length;
            string spinner = _spinnerFrames[spinnerIndex];

            string barContent = filled switch
            {
                var f when f >= barWidth => new string('#', barWidth),
                var f when f > 0 => new string('#', f - 1) + spinner + new string('-', barWidth - f),
                _ => spinner + new string('-', barWidth - 1)
            };

            sb.AppendLine($"{labels.Progress}: [{barContent}] {prog * 100,4:0.0}%");
            sb.AppendLine(line);

            // 4. スケジュール概要
            sb.AppendLine(_isJapanese ? "▼ 登録済みスケジュール" : "▼ REGISTERED SCHEDULES");
            sb.Append(config.GetScheduleSummary());
            sb.AppendLine();
            sb.AppendLine(line);

            // 5. 周期設定
            var fMin = config.Modes.GetValueOrDefault("Focus")?.Min ?? 0;
            var rMin = config.Modes.GetValueOrDefault("Rest")?.Min ?? 0;
            sb.AppendLine($"  (Loop: {fMin}m / {rMin}m)");
            sb.AppendLine(line);

            // 6. 物理演算パラメータ (Physics)
            string secUnit = _isJapanese ? "秒" : "s ";

            // 太さ
            sb.AppendLine($"{labels.ThickTitle}: {p.Thick,5:0.0} px  / {labels.SettingLabel}:{s.CurrentSet.Thick,12:0.0} px");
            // ぼかし
            sb.AppendLine($"{labels.BlurTitle}: {p.Blur,5:0.0} px  / {labels.RangeLabel}:{s.CurrentSet.BlurMin,5:0.0}-{s.CurrentSet.BlurMax,6:0.0} px");
            // 周期
            sb.AppendLine($"{labels.PulseTitle}: {p.PulseTime,5:0.0} {secUnit}  / {labels.CycleLabel}:{p.PulseSec,12:0.0} {secUnit}");
            // 不透明度
            sb.AppendLine($"{labels.OpTitle}: {p.Opacity * 100,5:0.0} %   / {labels.RangeLabel}:       {s.CurrentSet.OpMin * 100:0}-{s.CurrentSet.OpMax * 100:0} %");
            // 流速
            double currentFlowSec = p.Flow * s.CurrentSet.FlowDuration;
            sb.AppendLine($"{labels.FlowTitle}: {currentFlowSec,5:0.0} {secUnit}  / {labels.CycleLabel}:{s.CurrentSet.FlowDuration,12:0.0} {secUnit}");

            // 7. ステータス・遷移
            sb.AppendLine(line);
            sb.AppendLine($"{labels.Osc}: {p.Osc * 100,5:0.0} %");

            string transText = s.GetTransitionText(_isJapanese, config.TransitionSec);
            if (!string.IsNullOrEmpty(transText)) sb.AppendLine(transText);

            return sb.ToString();
        }

        /// <summary>
        /// 例外が発生した際に、時間とエラー内容をファイルに追記します
        /// </summary>
        public void LogError(string context, Exception ex)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"--- ERROR: {DateTime.Now:yyyy/MM/dd HH:mm:ss} ---");
                sb.AppendLine($"Context: {context}");
                sb.AppendLine($"Message: {ex.Message}");
                sb.AppendLine($"StackTrace: {ex.StackTrace}");
                sb.AppendLine(new string('=', 40));

                // ファイルに追記（なければ作成）
                File.AppendAllText(_logPath, sb.ToString());
            }
            catch
            {
                // ログの書き込み自体が失敗した場合は、どうしようもないので無視
            }
        }
    }
}