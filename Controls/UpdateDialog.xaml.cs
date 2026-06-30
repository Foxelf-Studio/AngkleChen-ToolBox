using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using 陈叔叔工具箱.Helpers;

namespace 陈叔叔工具箱.Controls;

public partial class UpdateDialog : Window
{
    private readonly UpdateCheckResult _result;
    private readonly UpdateChecker _checker;
    private bool _isUpdating;

    public UpdateDialog(UpdateCheckResult result)
    {
        InitializeComponent();
        _result = result;

        // TODO: 替换为实际的 GitHub 用户名
        _checker = new UpdateChecker("Foxelf-Studio", "AngkleChen-ToolBox");

        // 填充信息
        TxtCurrentVersion.Text = result.CurrentVersion;
        TxtLatestVersion.Text = $"v{result.LatestVersion}";
        TxtChangelog.Text = string.IsNullOrEmpty(result.Changelog) ? "暂无更新说明" : result.Changelog;

        // 如果没有增量更新清单，禁用增量更新选项
        if (result.Manifest == null || result.ChangedFiles.Count == 0)
        {
            RadioIncremental.IsEnabled = false;
            RadioIncremental.Content = "增量更新（不可用，将下载完整安装包）";
            RadioFull.IsChecked = true;
        }
    }

    private async void OnUpdateClick(object sender, RoutedEventArgs e)
    {
        if (_isUpdating) return;
        _isUpdating = true;

        BtnUpdate.IsEnabled = false;
        RadioIncremental.IsEnabled = false;
        RadioFull.IsEnabled = false;

        ProgressPanel.Visibility = Visibility.Visible;
        ButtonPanel.HorizontalAlignment = HorizontalAlignment.Stretch;

        try
        {
            if (RadioIncremental.IsChecked == true && _result.Manifest != null)
            {
                await PerformIncrementalUpdate();
            }
            else
            {
                await PerformFullUpdate();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"更新失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            ResetUI();
        }
    }

    private async Task PerformIncrementalUpdate()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var totalFiles = _result.ChangedFiles.Count;
        var completedFiles = 0;

        foreach (var file in _result.ChangedFiles)
        {
            var destPath = Path.Combine(baseDir, file.RelativePath);
            var downloadUrl = file.DownloadUrl;

            // 如果是相对路径，构造完整的 GitHub URL
            if (!downloadUrl.StartsWith("http"))
            {
                // TODO: 替换为实际的 GitHub 仓库地址
                downloadUrl = $"https://github.com/Foxelf-Studio/AngkleChen-ToolBox/releases/download/v{_result.LatestVersion}/{downloadUrl}";
            }

            TxtProgress.Text = $"正在更新 ({completedFiles + 1}/{totalFiles}): {file.RelativePath}";
            UpdateProgress(completedFiles, totalFiles);

            var progress = new Progress<double>(p =>
            {
                // 单文件进度（可选显示）
            });

            var success = await _checker.DownloadFileAsync(downloadUrl, destPath, progress);
            if (!success)
            {
                throw new Exception($"下载文件失败: {file.RelativePath}");
            }

            completedFiles++;
        }

        UpdateProgress(totalFiles, totalFiles);
        TxtProgress.Text = "更新完成！";

        MessageBox.Show("更新完成！程序将重新启动。", "更新成功", MessageBoxButton.OK, MessageBoxImage.Information);

        // 重启程序
        RestartApplication();
    }

    private async Task PerformFullUpdate()
    {
        // 下载完整安装包
        var releaseUrl = _result.ReleaseUrl;
        if (!string.IsNullOrEmpty(releaseUrl))
        {
            TxtProgress.Text = "正在打开下载页面...";

            // 打开 GitHub Release 页面下载完整安装包
            Process.Start(new ProcessStartInfo(releaseUrl)
            {
                UseShellExecute = true
            });

            TxtProgress.Text = "请在浏览器中下载完整安装包";
            MessageBox.Show("已在浏览器中打开下载页面，请下载完整安装包后手动安装。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void UpdateProgress(int current, int total)
    {
        if (total > 0)
        {
            var percentage = (double)current / total;
            ProgressBar.Width = ProgressPanel.ActualWidth * percentage;
        }
    }

    private void RestartApplication()
    {
        // 单文件应用使用 BaseDirectory
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var exePath = Path.Combine(baseDir, "陈叔叔工具箱.exe");

        if (!File.Exists(exePath))
        {
            // 尝试查找当前目录下的 exe 文件
            var files = Directory.GetFiles(baseDir, "*.exe");
            exePath = files.FirstOrDefault() ?? exePath;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true
        });

        Application.Current.Shutdown();
    }

    private void ResetUI()
    {
        _isUpdating = false;
        BtnUpdate.IsEnabled = true;
        RadioIncremental.IsEnabled = _result.Manifest != null && _result.ChangedFiles.Count > 0;
        RadioFull.IsEnabled = true;
        ProgressPanel.Visibility = Visibility.Collapsed;
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
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            DragMove();
    }
}
