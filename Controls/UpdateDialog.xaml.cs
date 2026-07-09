using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using 陈叔叔工具箱.Helpers;

namespace 陈叔叔工具箱.Controls;

public partial class UpdateDialog : Window
{
    private readonly UpdateCheckResult _result;

    public UpdateDialog(UpdateCheckResult result)
    {
        InitializeComponent();
        _result = result;

        TxtCurrentVersion.Text = $"v{result.CurrentVersion}";
        TxtLatestVersion.Text = $"v{result.LatestVersion}";
        TxtChangelog.Text = string.IsNullOrEmpty(result.Changelog) ? "暂无更新说明" : result.Changelog;
    }

    private void OnUpdateClick(object sender, RoutedEventArgs e)
    {
        // 打开 GitHub Release 页面
        if (!string.IsNullOrEmpty(_result.ReleaseUrl))
        {
            Process.Start(new ProcessStartInfo(_result.ReleaseUrl)
            {
                UseShellExecute = true
            });
        }
        DialogResult = true;
        Close();
    }

    private void OnRemindLaterClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }
}
