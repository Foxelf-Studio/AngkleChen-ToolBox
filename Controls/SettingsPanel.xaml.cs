using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using 陈叔叔工具箱.Helpers;

namespace 陈叔叔工具箱.Controls;

public partial class SettingsPanel : UserControl
{
    private readonly UpdateChecker _checker;
    private bool _isChecking;

    public event EventHandler? CloseRequested;

    /// <summary>回收站操作改变了工具清单，通知主窗口刷新</summary>
    public event EventHandler? ToolsChanged;

    private AppConfig? _config;

    public SettingsPanel()
    {
        InitializeComponent();

        _checker = new UpdateChecker("Foxelf-Studio", "AngkleChen-ToolBox");

        // 显示当前版本
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        TxtVersion.Text = $"v{version?.ToString(3) ?? "1.1.0"}";

        // 加载设置
        LoadSettings();
    }

    /// <summary>
    /// 绑定主窗口的配置实例（避免多处 AppConfig 各写各的）
    /// </summary>
    public void Bind(AppConfig config)
    {
        _config = config;
        RefreshTrash();
    }

    public void RefreshTrash()
    {
        var count = _config?.Trash.Count ?? 0;

        if (count == 0)
        {
            TxtTrashStatus.Text = "回收站为空";
            BtnRestoreLast.IsEnabled = false;
            BtnEmptyTrash.IsEnabled = false;
            return;
        }

        TxtTrashStatus.Text = $"共 {count} 项 · 最近：{_config!.Trash[0].Name}";
        BtnRestoreLast.IsEnabled = true;
        BtnEmptyTrash.IsEnabled = true;
    }

    private void OnRestoreLastClick(object sender, RoutedEventArgs e)
    {
        if (_config == null || _config.Trash.Count == 0) return;

        var entry = _config.Trash[0];
        if (_config.RestoreFromTrash(entry.Id))
        {
            RefreshTrash();
            ToolsChanged?.Invoke(this, EventArgs.Empty);
            TxtTrashStatus.Text = $"已恢复：{entry.Name}";
        }
        else
        {
            TxtTrashStatus.Text = "恢复失败，文件可能被占用";
        }
    }

    private void OnEmptyTrashClick(object sender, RoutedEventArgs e)
    {
        if (_config == null || _config.Trash.Count == 0) return;

        var result = CustomMessageBox.Show(
            $"回收站中有 {_config.Trash.Count} 项，清空后无法恢复。\n\n确定要清空吗？",
            "清空回收站",
            MessageBoxButton.YesNo);

        if (result != MessageBoxResult.Yes) return;

        _config.EmptyTrash();
        RefreshTrash();
        ToolsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string AppDirectory => AppPaths.Root;

    private void LoadSettings()
    {
        // 读写收敛到 AppSettings，WinUI 版共用同一份实现
        var settings = AppSettings.Load();

        ToggleAutoCheck.IsChecked = settings.AutoCheckUpdate;
        ToggleLogEnabled.IsChecked = settings.EnableLog;
        Logger.IsEnabled = settings.EnableLog;
    }

    private void SaveSettings()
    {
        new AppSettings
        {
            AutoCheckUpdate = ToggleAutoCheck.IsChecked == true,
            EnableLog = ToggleLogEnabled.IsChecked == true
        }.Save();
    }

    private string? _releaseUrl;

    private async void OnCheckUpdateClick(object sender, RoutedEventArgs e)
    {
        // 如果已经有更新，直接打开浏览器
        if (_releaseUrl != null)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_releaseUrl)
            {
                UseShellExecute = true
            });
            return;
        }

        if (_isChecking) return;
        _isChecking = true;

        BtnCheckUpdate.IsEnabled = false;
        TxtUpdateStatus.Text = "正在检查更新...";
        TxtUpdateStatus.Foreground = System.Windows.Media.Brushes.White;

        try
        {
            var result = await _checker.CheckForUpdatesAsync();

            if (result.IsError)
            {
                TxtUpdateStatus.Text = "检查失败，请重启软件或更换网络再试";
                TxtUpdateStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(255, 100, 100));
            }
            else if (result.HasUpdate)
            {
                _releaseUrl = result.ReleaseUrl;
                TxtUpdateStatus.Text = $"发现新版本 v{result.LatestVersion}";
                TxtUpdateStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(96, 205, 255)); // #60cdff
                BtnCheckUpdate.Content = "查看更新";
                BtnCheckUpdate.IsEnabled = true;
            }
            else
            {
                TxtUpdateStatus.Text = $"当前已是最新版本 (v{result.CurrentVersion})";
                TxtUpdateStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(170, 170, 170)); // #aaaaaa
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"检查更新异常: {ex.Message}");
            TxtUpdateStatus.Text = "检查失败，请重启软件或更换网络再试";
            TxtUpdateStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 100, 100));
        }
        finally
        {
            if (_releaseUrl == null)
                BtnCheckUpdate.IsEnabled = true;
            _isChecking = false;
        }
    }

    private void OnAutoCheckChanged(object sender, RoutedEventArgs e)
    {
        SaveSettings();
    }

    private void OnLogEnabledChanged(object sender, RoutedEventArgs e)
    {
        Logger.IsEnabled = ToggleLogEnabled.IsChecked == true;
        SaveSettings();
    }

    private void OnGitHubClick(object sender, RoutedEventArgs e)
    {
        try
        {
            // TODO: 替换为实际的 GitHub 仓库地址
            Process.Start(new ProcessStartInfo("https://github.com/Foxelf-Studio/AngkleChen-ToolBox")
            {
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
