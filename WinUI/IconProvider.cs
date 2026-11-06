using System.Collections.Concurrent;
using System.IO;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using 陈叔叔工具箱.Helpers;

namespace AngkleChen;

/// <summary>
/// WinUI 侧的图标加载：字节来源与 WPF 版共用 <see cref="IconExtractor"/>。
///
/// 两段职责分开是有原因的：
/// · 取字节（磁盘缓存 / Shell 提取）是纯 IO，放后台线程
/// · <see cref="BitmapImage"/> 是 DependencyObject，**必须在 UI 线程创建**，
///   否则对象能建成功但渲染不出来（表现为部分卡片一直空着）
/// </summary>
public static class IconProvider
{
    private static readonly ConcurrentDictionary<string, byte[]> ByteCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly SemaphoreSlim Gate = new(4);

    /// <summary>取图标 PNG 字节。线程无关，可放心在后台调用。</summary>
    public static async Task<byte[]?> LoadBytesAsync(string relativePath)
    {
        if (ByteCache.TryGetValue(relativePath, out var cached)) return cached;

        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (ByteCache.TryGetValue(relativePath, out cached)) return cached;

            var bytes = await Task.Run(() => ReadBytes(relativePath)).ConfigureAwait(false);
            if (bytes != null) ByteCache[relativePath] = bytes;
            return bytes;
        }
        catch
        {
            return null;
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>字节转位图。**只能在 UI 线程调用。**</summary>
    public static async Task<ImageSource?> ToBitmapAsync(byte[] bytes)
    {
        try
        {
            using var stream = new InMemoryRandomAccessStream();

            using (var writer = new DataWriter(stream))
            {
                writer.WriteBytes(bytes);
                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachStream();
            }

            stream.Seek(0);

            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(stream);
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static byte[]? ReadBytes(string relativePath)
    {
        try
        {
            var cachePath = AppConfig.GetIconCachePath(relativePath);
            if (File.Exists(cachePath)) return File.ReadAllBytes(cachePath);

            // 磁盘没有缓存就现提一次，顺手写回缓存，下次不用再问 Shell
            var full = Path.Combine(AppPaths.Root, relativePath);
            var bytes = IconExtractor.ToPngBytes(full);
            if (bytes == null) return null;

            try
            {
                var dir = Path.GetDirectoryName(cachePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(cachePath, bytes);
            }
            catch
            {
                // 缓存写不进去不影响显示
            }

            return bytes;
        }
        catch
        {
            return null;
        }
    }
}
