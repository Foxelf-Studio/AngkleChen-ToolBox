using System.Windows;
using System.Windows.Threading;

namespace 陈叔叔工具箱;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Win11 暗色模式
        Current.Resources.MergedDictionaries.Clear();
    }
}
