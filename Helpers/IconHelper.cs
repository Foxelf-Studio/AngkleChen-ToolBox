using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace 陈叔叔工具箱.Helpers;

/// <summary>
/// WPF 侧的图标入口：把 <see cref="IconExtractor"/> 产出的 PNG 字节包装成 <see cref="ImageSource"/>。
/// 与提取逻辑分离，提取部分两个 UI 框架共用，这里只做平台适配。
/// </summary>
public static class IconHelper
{
    private static readonly ConcurrentDictionary<string, ImageSource> _cache = new();
    private static string _toolboxRoot = "";

    public static void Init(string root) => _toolboxRoot = root;

    /// <summary>
    /// 清空内存中的图标缓存。磁盘上的缓存图被重新提取后调用，
    /// 否则界面会一直显示旧图标直到重启。
    /// </summary>
    public static void ClearCache() => _cache.Clear();

    public static ImageSource GetIcon(string relativePath)
    {
        var fullPath = Path.Combine(_toolboxRoot, relativePath);

        return _cache.GetOrAdd(fullPath, _ =>
        {
            // 优先用磁盘缓存，省一次 Shell 调用
            var cachePath = AppConfig.GetIconCachePath(relativePath);
            if (File.Exists(cachePath))
            {
                try { return FromPngBytes(File.ReadAllBytes(cachePath)); }
                catch { }
            }

            var bytes = IconExtractor.ToPngBytes(fullPath);
            return bytes != null ? FromPngBytes(bytes) : Placeholder();
        });
    }

    private static ImageSource FromPngBytes(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    /// <summary>透明 1×1 占位，保证绑定时不会拿到 null</summary>
    private static ImageSource Placeholder()
    {
        var bitmap = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);
        bitmap.Freeze();
        return bitmap;
    }
}
