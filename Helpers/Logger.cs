using System.IO;

namespace 陈叔叔工具箱.Helpers;

/// <summary>
/// 简单的日志记录器
/// </summary>
public static class Logger
{
    private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
    private static readonly object _lock = new();

    /// <summary>
    /// 是否启用日志记录
    /// </summary>
    public static bool IsEnabled { get; set; } = false;

    public static void Log(string message)
    {
        if (!IsEnabled) return;

        lock (_lock)
        {
            try
            {
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
                File.AppendAllText(LogPath, line);
            }
            catch { }
        }
    }

    public static void Clear()
    {
        try
        {
            if (File.Exists(LogPath))
                File.Delete(LogPath);
        }
        catch { }
    }
}
