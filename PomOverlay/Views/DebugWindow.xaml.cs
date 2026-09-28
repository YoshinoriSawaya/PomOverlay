using System;
using System.Windows;

namespace PomOverlay
{
    // モニター左上に出すデバッグ情報のウィンドウ
    public partial class DebugWindow : Window
    {
        public DebugWindow()
        {
            InitializeComponent();
        }

        public void SetText(string text) => DebugText.Text = text;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ClickThrough.Apply(this);
        }
    }
}
