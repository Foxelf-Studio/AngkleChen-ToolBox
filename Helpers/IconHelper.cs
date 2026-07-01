using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace 陈叔叔工具箱.Helpers;

public static class IconHelper
{
    private static readonly ConcurrentDictionary<string, ImageSource> _cache = new();
    private static string _toolboxRoot = "";

    public static void Init(string root) => _toolboxRoot = root;

    public static ImageSource GetIcon(string relativePath)
    {
        var fullPath = System.IO.Path.Combine(_toolboxRoot, relativePath);
        return _cache.GetOrAdd(fullPath, ExtractIcon);
    }

    // 需要使用 Edge 图标的 bat 文件路径关键词
    private static readonly string[] _edgeIconKeywords = [
        "游戏平台", "AMD显卡驱动下载", "Nvidia显卡驱动下载",
        "MSIAfterburnerSetup", "UFO测试", "在线屏幕测试",
        "在线外设测试中心", "next_itellyou"
    ];

    private static ImageSource ExtractIcon(string path)
    {
        try
        {
            // 对于打开网页的 .bat 文件，使用 Edge 浏览器图标
            var iconPath = path;
            if (System.IO.Path.GetExtension(path).Equals(".bat", StringComparison.OrdinalIgnoreCase))
            {
                if (_edgeIconKeywords.Any(k => path.Contains(k)))
                {
                    var edgePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
                    if (System.IO.File.Exists(edgePath))
                        iconPath = edgePath;
                }
            }

            var info = new SHFILEINFO();
            SHGetFileInfo(iconPath, 0, ref info, (uint)Marshal.SizeOf<SHFILEINFO>(),
                SHGFI_ICON | SHGFI_LARGEICON);

            if (info.hIcon != IntPtr.Zero)
            {
                var source = Imaging.CreateBitmapSourceFromHIcon(
                    info.hIcon, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                DestroyIcon(info.hIcon);
                source.Freeze();
                return source;
            }
        }
        catch { }

        var fb = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);
        fb.Freeze();
        return fb;
    }

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
