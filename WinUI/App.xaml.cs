using System.IO;
using Microsoft.UI.Xaml;

namespace AngkleChen;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();

        // 未处理异常不写日志的话，0xc000027b 这种崩溃只能靠猜。
        // 这里落一份 crash.log，下次出问题能直接看到异常类型和堆栈。
        UnhandledException += (_, e) =>
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(AppContext.BaseDirectory, "crash.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.Exception}{Environment.NewLine}{Environment.NewLine}");
            }
            catch
            {
                // 日志写不进去也不能再抛
            }
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
