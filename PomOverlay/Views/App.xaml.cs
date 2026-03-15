using PomOverlay.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Forms;
using static PomOverlay.AppConfig;

namespace PomOverlay
{
    public partial class App : System.Windows.Application
    {
        private AppConfig _config = new();
        private List<MainWindow> _windows = new();
        private NotifyIcon _notifyIcon = null!;
        private bool _isJapanese = true;
        private bool _showDebug = true;

        //private System.Drawing.Icon? _iconNormal;
        //private System.Drawing.Icon? _iconSleep;


        // NotifyIconを管理しているクラス（App.xaml.cs等）に追加
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint RegisterWindowMessage(string lpString);

        private uint _uTaskbarRestartMsg;
        // App.xaml.cs 内
        private readonly DebugManager _logger = new(); // ログ用

        protected override void OnStartup(StartupEventArgs e)
        {
            // 1. 設定の読み込み
            LoadOrCreateConfig();

            //this.DispatcherUnhandledException += (s, e) => {
            //    System.Windows.MessageBox.Show($"致命的なエラー: {e.Exception.Message}");
            //    e.Handled = true; // アプリを落とさずに継続を試みる
            //};
            // 未処理の例外をキャッチするイベント
            this.DispatcherUnhandledException += (s, ex) =>
            {
                _logger.LogError("Unhandled Exception", ex.Exception);

                // ユーザーに通知（これがないと黙って消える）
                System.Windows.MessageBox.Show("予期しないエラーが発生しました。error.log を確認してください。", "PomOverlay Error");

                // trueにすると、アプリを落とさずに続行を試みる
                ex.Handled = true;
            };
            // タスクバーが再作成された時のメッセージIDを取得
            _uTaskbarRestartMsg = RegisterWindowMessage("TaskbarCreated");

            // 2. 全ディスプレイにウィンドウを配置
            var screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                var screen = screens[i];
                var rect = new Rect(screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height);

                var win = new MainWindow(rect, i, _config);
                win.SetLanguage(_isJapanese);
                win.SetDebugVisibility(i == 1 && _showDebug); // 最初は1番目のみデバッグ表示

                //win.SetDebugVisibility(i == 0 && _showDebug); // 最初は0番目のみデバッグ表示

                win.Show();
                _windows.Add(win);
            }

            // 3. タスクトレイの設定
            SetupTrayIcon();
        }


        // メッセージループで再起動を検知（HwndSourceなどを使用している場合）
        // もし MainWindow があるなら、その HwndSourceHook で処理します


        private void UpdateTrayIcon(AppConfig.Mode mode)
        {
            if (_notifyIcon == null) return;

            // モードに応じてアイコンやテキストを切り替える
            switch (mode)
            {
                case AppConfig.Mode.Sleep:
                    //_notifyIcon.Icon = _iconNormal;
                    _notifyIcon.Text = "PomOverlay - 睡眠中 🌙";
                    break;
                case AppConfig.Mode.Focus:
                    //_notifyIcon.Icon = _iconNormal;
                    _notifyIcon.Text = "PomOverlay - 集中モード 🎯";
                    break;
                default:
                    //_notifyIcon.Icon = _iconNormal;
                    _notifyIcon.Text = "PomOverlay - 自動運転中";
                    break;
            }
        }

        private void LoadOrCreateConfig()
        {
            string path = "config.json";
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    _config = JsonSerializer.Deserialize<AppConfig>(json) ?? AppConfig.CreateDefault();
                }
                else
                {
                    _config = AppConfig.CreateDefault();
                    SaveConfig();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"設定の読み込みに失敗しました。デフォルトを使用します。\n{ex.Message}",
                    "PomOverlay",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                    );
                _config = AppConfig.CreateDefault();
            }
        }

        private void SaveConfig()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                // ここがポイント：日本語をエスケープせずにそのまま出力する
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }; string json = JsonSerializer.Serialize(_config, options);
            File.WriteAllText("config.json", json);
        }

        private void SetupTrayIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();

            try
            {
                // 実行ファイルのフォルダにある favicon.ico を直接読み込む
                // ※このとき、icoファイルのプロパティで「出力ディレクトリにコピー」を「常にコピー」にする必要があります
                _notifyIcon.Icon = new System.Drawing.Icon("favicon.ico");
            }
            catch (Exception ex)
            {
                // 読み込めなかった時のためのバックアップ
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                System.Diagnostics.Debug.WriteLine($"アイコン読み込み失敗: {ex.Message}");
            }

            _notifyIcon.Visible = true;
            _notifyIcon.Text = "PomOverlay";
            var menu = new ContextMenuStrip();

            // --- 【新設】モード切替セクション ---
            var modeMenu = new ToolStripMenuItem("現在のモードを固定");

            // 各モードのアイテム作成（ヘルパーメソッドを使うとスッキリします）
            modeMenu.DropDownItems.Add(CreateOverrideItem("自動 (通常サイクル)", AppConfig.Mode.Auto));

            modeMenu.DropDownItems.Add(new ToolStripSeparator());
            modeMenu.DropDownItems.Add(CreateOverrideItem("強制 Focus 🎯", AppConfig.Mode.Focus));
            modeMenu.DropDownItems.Add(CreateOverrideItem("強制 Rest ☕", AppConfig.Mode.Rest));
            modeMenu.DropDownItems.Add(CreateOverrideItem("強制 Sleep 🌙", AppConfig.Mode.Sleep));

            // modeMenu.DropDownOpening の中を以下のように修正
            modeMenu.DropDownOpening += (s, e) =>
            {
                foreach (ToolStripItem item in modeMenu.DropDownItems)
                {
                    // セパレーターを無視して、ToolStripMenuItem だけを処理する
                    if (item is ToolStripMenuItem menuItem && menuItem.Tag is AppConfig.Mode m)
                    {
                        menuItem.Checked = (_config.OverrideMode == m);
                    }
                }
            };

            menu.Items.Add(modeMenu);
            menu.Items.Add(new ToolStripSeparator());



            // --- 設定操作系 ---
            menu.Items.Add("設定ファイル (JSON) を開く", null, (s, e) =>
            {
                if (File.Exists("config.json"))
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("config.json") { UseShellExecute = true });
            });

            menu.Items.Add("設定をリロード", null, (s, e) => ReloadConfig());

            menu.Items.Add("設定を初期値に戻す", null, (s, e) =>
            {
                var yesNo = System.Windows.MessageBox.Show(
                    "設定を初期値に戻して上書きしますか？",
                    "PomOverlay",
                    MessageBoxButton.YesNo);

                if (yesNo == MessageBoxResult.Yes)
                {
                    _config = AppConfig.CreateDefault();
                    SaveConfig();
                    ApplyConfigToWindows();
                    System.Windows.MessageBox.Show(
                        "初期化しました。",
                        "PomOverlay",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            });

            menu.Items.Add(new ToolStripSeparator());

            // --- 表示設定系 ---
            menu.Items.Add("言語切替 (JP/EN)", null, (s, e) =>
            {
                _isJapanese = !_isJapanese;
                _windows.ForEach(w => w.SetLanguage(_isJapanese));
            });

            // デバッグ表示用サブメニュー
            var debugMenu = new ToolStripMenuItem("デバッグ表示設定");
            for (int i = 0; i < _windows.Count; i++)
            {
                int index = i;
                var item = new ToolStripMenuItem($"スクリーン {i}") { Checked = _windows[i].IsDebugVisible };
                item.Click += (s, e) =>
                {
                    bool next = !item.Checked;
                    _windows[index].SetDebugVisibility(next);
                    item.Checked = next;
                };
                debugMenu.DropDownItems.Add(item);
            }
            menu.Items.Add(debugMenu);

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("終了", null, (s, e) => Shutdown());

            _notifyIcon.ContextMenuStrip = menu;
        }

        private void ReloadConfig()
        {
            LoadOrCreateConfig();
            ApplyConfigToWindows();
            // メッセージ, タイトル, ボタン, アイコン
            System.Windows.MessageBox.Show(
                "設定を再読み込みしました。",
                "PomOverlay",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        // アイテム作成を共通化するヘルパー
        // App.xaml.cs 内
        private ToolStripMenuItem CreateOverrideItem(string text, AppConfig.Mode mode)
        {
            var item = new ToolStripMenuItem(text) { Tag = mode };
            item.Click += (s, e) =>
            {
                _config.OverrideMode = mode; // enum を代入
                ApplyConfigToWindows();
            };
            return item;
        }

        private void ApplyConfigToWindows()
        {
            _windows.ForEach(w => w.UpdateConfig(_config));
            // タスクトレイの状態も更新
            UpdateTrayIcon(_config.OverrideMode);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
