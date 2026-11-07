using System.Runtime.InteropServices;

namespace AngkleChen;

/// <summary>
/// 光标强制复位。
///
/// 现象：右键卡片弹出菜单后不动鼠标，光标会停在「忙」的转圈形状，
/// 鼠标一动就恢复。
///
/// 原因是弹出层是独立窗口，它不主动设置光标时，
/// 系统会沿用上一个窗口留下的形状。这里在菜单开关后显式把光标设回箭头。
/// </summary>
internal static class NativeCursor
{
    private const int IdcArrow = 32512;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr LoadCursorW(IntPtr hInstance, IntPtr lpCursorName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetCursor(IntPtr hCursor);

    private static readonly IntPtr Arrow = LoadCursorW(IntPtr.Zero, IdcArrow);

    public static void ForceArrow()
    {
        if (Arrow != IntPtr.Zero)
            SetCursor(Arrow);
    }
}
