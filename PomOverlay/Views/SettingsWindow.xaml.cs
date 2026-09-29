using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PomOverlay
{
    // 設定ウィンドウ。編集内容と入力チェックは SettingsViewModel が持ち、ここは画面との配線だけ
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;
        private readonly Action<AppConfig> _apply;

        public SettingsWindow(SettingsViewModel viewModel, Action<AppConfig> apply)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _apply = apply;
            DataContext = viewModel;
        }

        private void OnApply(object sender, RoutedEventArgs e)
        {
            SchedulesGrid.CommitEdit(DataGridEditingUnit.Row, true);

            // 数値として読めない入力は ViewModel まで届かないので、画面側で先に止める
            if (HasBindingError(this))
            {
                StatusText.Text = "赤枠の欄に数値として読めない入力があります";
                return;
            }

            if (!_viewModel.TryBuildConfig(out var config, out var errors))
            {
                System.Windows.MessageBox.Show(this,
                    "次の値を直してから適用してください。\n\n" + string.Join("\n", errors),
                    "PomOverlay", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusText.Text = "適用していません";
                return;
            }

            _apply(config);
            StatusText.Text = $"適用しました ({DateTime.Now:HH:mm:ss})";
        }

        private void OnClose(object sender, RoutedEventArgs e) => Close();

        private void OnAddSchedule(object sender, RoutedEventArgs e)
        {
            SchedulesGrid.SelectedItem = _viewModel.AddSchedule();
        }

        private void OnRemoveSchedule(object sender, RoutedEventArgs e)
        {
            if (SchedulesGrid.SelectedItem is ScheduleSettingsViewModel item)
            {
                SchedulesGrid.CommitEdit(DataGridEditingUnit.Row, true);
                _viewModel.RemoveSchedule(item);
            }
        }

        private static bool HasBindingError(DependencyObject root)
        {
            if (Validation.GetHasError(root)) return true;
            return Enumerable.Range(0, VisualTreeHelper.GetChildrenCount(root))
                .Any(i => HasBindingError(VisualTreeHelper.GetChild(root, i)));
        }
    }
}
