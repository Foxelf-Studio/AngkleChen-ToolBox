using System.Collections.Concurrent;
using System.IO;
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
        var fullPath = Path.Combine(_toolboxRoot, relativePath);

        // 先尝试从缓存文件加载
        var cachePath = AppConfig.GetIconCachePath(relativePath);
        if (File.Exists(cachePath))
        {
            return _cache.GetOrAdd(fullPath, _ => LoadIconFromFile(cachePath));
        }

        return _cache.GetOrAdd(fullPath, ExtractIcon);
    }

    /// <summary>
    /// 从缓存文件加载图标
    /// </summary>
    private static ImageSource LoadIconFromFile(string filePath)
    {
        try
        {
            var bytes = File.ReadAllBytes(filePath);
            using var stream = new MemoryStream(bytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            var fb = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);
            fb.Freeze();
            return fb;
        }
    }

    /// <summary>
    /// 提取图标并返回 PNG 字节数组（用于缓存）
    /// </summary>
    public static byte[]? ExtractIconToFile(string path)
    {
        try
        {
            // 对于打开网页的 .bat 文件，使用 Edge 浏览器图标
            var iconPath = path;
            if (Path.GetExtension(path).Equals(".bat", StringComparison.OrdinalIgnoreCase))
            {
                if (_edgeIconKeywords.Any(k => path.Contains(k)))
                {
                    var edgePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
                    if (File.Exists(edgePath))
                        iconPath = edgePath;
                }
            }

            var info = new SHFILEINFO();
            SHGetFileInfo(iconPath, 0, ref info, (uint)Marshal.SizeOf<SHFILEINFO>(),
                SHGFI_ICON | SHGFI_LARGEICON);

            if (info.hIcon != IntPtr.Zero)
            {
                using var icon = System.Drawing.Icon.FromHandle(info.hIcon);
                using var bitmap = icon.ToBitmap();
                using var stream = new MemoryStream();
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                DestroyIcon(info.hIcon);
                return stream.ToArray();
            }
        }
        catch { }

        return null;
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
            if (Path.GetExtension(path).Equals(".bat", StringComparison.OrdinalIgnoreCase))
            {
                if (_edgeIconKeywords.Any(k => path.Contains(k)))
                {
                    var edgePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
                    if (File.Exists(edgePath))
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
