using System.IO;
using System.Runtime.InteropServices;

namespace 陈叔叔工具箱.Helpers;

/// <summary>
/// 从文件/目录提取图标，只负责产出 PNG 字节，不碰任何 UI 类型。
///
/// 之所以单独拆出来：WPF 和 WinUI 3 的图标类型不通用
/// （<c>System.Windows.Media.ImageSource</c> vs <c>Microsoft.UI.Xaml.Media.ImageSource</c>），
/// 但「问 Windows 要这个文件的图标」这件事两边完全一样。
/// 平台相关的那一层留给各自的 IconHelper 做，这里只管字节。
/// </summary>
public static class IconExtractor
{
    /// <summary>
    /// 提取图标并编码成 PNG 字节。失败返回 null（调用方决定怎么兜底）。
    /// </summary>
    public static byte[]? ToPngBytes(string path)
    {
        var handle = LoadIconHandle(path);
        if (handle == IntPtr.Zero) return null;

        try
        {
            using var icon = System.Drawing.Icon.FromHandle(handle);
            using var bitmap = icon.ToBitmap();
            using var stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            return stream.ToArray();
        }
        catch
        {
            return null;
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    /// <summary>
    /// 向 Shell 要图标句柄，调用方负责 DestroyIcon。
    /// </summary>
    public static IntPtr LoadIconHandle(string path)
    {
        try
        {
            var info = new SHFILEINFO();
            SHGetFileInfo(ResolveIconSource(path), 0, ref info, (uint)Marshal.SizeOf<SHFILEINFO>(),
                SHGFI_ICON | SHGFI_LARGEICON);
            return info.hIcon;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// .bat 里有一批是「打开网页」的快捷方式，用 Edge 的图标比通用图标更容易辨认。
    /// </summary>
    private static string ResolveIconSource(string path)
    {
        if (!Path.GetExtension(path).Equals(".bat", StringComparison.OrdinalIgnoreCase))
            return path;

        if (!EdgeIconKeywords.Any(k => path.Contains(k)))
            return path;

        const string edgePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
        return File.Exists(edgePath) ? edgePath : path;
    }

    private static readonly string[] EdgeIconKeywords = [
        "游戏平台", "AMD显卡驱动下载", "Nvidia显卡驱动下载",
        "MSIAfterburnerSetup", "UFO测试", "在线屏幕测试",
        "在线外设测试中心", "next_itellyou"
    ];

    // ── P/Invoke ────────────────────────────────────
    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_LARGEICON = 0x0;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath, uint dwFileAttributes,
        ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
