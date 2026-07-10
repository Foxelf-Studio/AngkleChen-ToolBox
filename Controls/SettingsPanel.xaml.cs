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

    public SettingsPanel()
    {
        InitializeComponent();

        // TODO: 替换为实际的 GitHub 用户名
        _checker = new UpdateChecker("Foxelf-Studio", "AngkleChen-ToolBox");

        // 显示当前版本
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        TxtVersion.Text = $"v{version?.ToString(3) ?? "1.1.0"}";

        // 加载设置
        LoadSettings();
    }

    private void LoadSettings()
    {
        // 从配置文件加载设置（简单实现，可扩展）
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                if (settings != null && settings.TryGetValue("autoCheckUpdate", out var autoCheck))
                {
                    ToggleAutoCheck.IsChecked = autoCheck;
                }
            }
            catch { }
        }
    }

    private void SaveSettings()
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        var settings = new Dictionary<string, bool>
        {
            ["autoCheckUpdate"] = ToggleAutoCheck.IsChecked == true
        };
        var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
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

            if (result.HasUpdate)
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
            TxtUpdateStatus.Text = "检查失败，请检查网络连接";
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
