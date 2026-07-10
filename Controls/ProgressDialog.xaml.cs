using System.IO;
using System.Windows;
using System.Windows.Input;

namespace 陈叔叔工具箱.Controls;

public partial class ProgressDialog : Window
{
    private readonly string _sourcePath;
    private readonly string _destPath;
    private readonly bool _isFolder;
    private CancellationTokenSource? _cts;

    public bool IsCompleted { get; private set; }
    public bool IsCancelled { get; private set; }

    public ProgressDialog(string sourcePath, string destPath, bool isFolder)
    {
        InitializeComponent();
        _sourcePath = sourcePath;
        _destPath = destPath;
        _isFolder = isFolder;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _cts = new CancellationTokenSource();

        try
        {
            if (_isFolder)
            {
                await CopyDirectoryAsync(_sourcePath, _destPath, _cts.Token);
            }
            else
            {
                await CopyFileAsync(_sourcePath, _destPath, _cts.Token);
            }

            IsCompleted = true;
            DialogResult = true;
            Close();
        }
        catch (OperationCanceledException)
        {
            IsCancelled = true;
            DialogResult = false;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            DialogResult = false;
            Close();
        }
    }

    private async Task CopyFileAsync(string source, string dest, CancellationToken ct)
    {
        var fileInfo = new FileInfo(source);
        var totalSize = fileInfo.Length;

        TxtStatus.Text = $"正在复制: {Path.GetFileName(source)}";

        // 确保目标目录存在
        var destDir = Path.GetDirectoryName(dest);
        if (destDir != null && !Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        await Task.Run(async () =>
        {
            var buffer = new byte[81920];
            long totalRead = 0;

            using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
            using var destStream = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

            int bytesRead;
            while ((bytesRead = await sourceStream.ReadAsync(buffer, ct)) > 0)
            {
                await destStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                totalRead += bytesRead;

                // 更新进度
                var progress = (double)totalRead / totalSize * 100;
                Dispatcher.Invoke(() =>
                {
                    ProgressBar.Width = (ActualWidth - 40) * (progress / 100);
                    TxtProgress.Text = $"{progress:F0}%";
                });
            }
        }, ct);
    }

    private async Task CopyDirectoryAsync(string sourceDir, string destDir, CancellationToken ct)
    {
        var dirInfo = new DirectoryInfo(sourceDir);
        var allFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);
        var totalSize = allFiles.Sum(f => f.Length);
        long totalCopied = 0;

        TxtStatus.Text = $"正在复制: {Path.GetFileName(sourceDir)}";

        await Task.Run(async () =>
        {
            foreach (var file in allFiles)
            {
                ct.ThrowIfCancellationRequested();

                var relativePath = Path.GetRelativePath(sourceDir, file.FullName);
                var destFile = Path.Combine(destDir, relativePath);

                // 确保目标目录存在
                var destFileDir = Path.GetDirectoryName(destFile);
                if (destFileDir != null && !Directory.Exists(destFileDir))
                    Directory.CreateDirectory(destFileDir);

                var buffer = new byte[81920];
                long fileRead = 0;

                using var sourceStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
                using var destStream = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

                int bytesRead;
                while ((bytesRead = await sourceStream.ReadAsync(buffer, ct)) > 0)
                {
                    await destStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                    fileRead += bytesRead;
                    totalCopied += bytesRead;

                    // 更新进度
                    var progress = (double)totalCopied / totalSize * 100;
                    Dispatcher.Invoke(() =>
                    {
                        ProgressBar.Width = (ActualWidth - 40) * (progress / 100);
                        TxtProgress.Text = $"{progress:F0}%";
                        TxtStatus.Text = $"正在复制: {relativePath}";
                    });
                }
            }
        }, ct);
    }

    private void TitleBar_Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _cts?.Cancel();
    }
}
