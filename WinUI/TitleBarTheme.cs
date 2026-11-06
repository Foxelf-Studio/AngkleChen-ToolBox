using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace AngkleChen;

/// <summary>
/// 让系统标题栏跟随应用深浅色。
///
/// WinUI 3 的无包应用，标题栏由 DWM 绘制，默认不读应用请求的主题：
/// 深色界面配一条白色标题栏，看起来像没渲染完。
/// 这里直接给窗口打 immersive dark mode 标记，主题变化时重打一次。
/// </summary>
internal static class TitleBarTheme
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeLegacy = 19;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void Apply(Window window, bool dark)
    {
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var value = dark ? 1 : 0;

            // 20 是 20H1+ 的编号，老系统用 19；两个都试一遍最省事
            if (DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref value, sizeof(int)) != 0)
                DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkModeLegacy, ref value, sizeof(int));
        }
        catch
        {
            // 打不上标记就维持系统默认，不影响功能
        }
    }
}
