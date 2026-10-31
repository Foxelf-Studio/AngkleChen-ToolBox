using System.Windows;
using System.Windows.Threading;

namespace 陈叔叔工具箱;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 兜底：未捕获异常写入日志，避免闪退后无任何线索
        DispatcherUnhandledException += (_, args) =>
        {
            Helpers.Logger.Log($"未处理异常: {args.Exception}");
        };
    }
}
