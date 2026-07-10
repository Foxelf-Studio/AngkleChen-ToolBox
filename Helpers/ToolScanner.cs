using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using 陈叔叔工具箱.Models;

namespace 陈叔叔工具箱.Helpers;

/// <summary>
/// 工具扫描器 - 自动扫描工具文件夹
/// </summary>
public static class ToolScanner
{
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools.json");
    private static readonly string ToolsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "工具");

    // 缓存的工具列表
    private static List<ToolInfo>? _cachedTools;

    /// <summary>
    /// 异步扫描工具（带缓存）
    /// </summary>
    public static async Task<List<ToolInfo>> ScanToolsAsync()
    {
        // 如果有缓存，直接返回
        if (_cachedTools != null)
            return _cachedTools;

        // 异步扫描，不阻塞UI
        return await Task.Run(() =>
        {
            var tools = new List<ToolInfo>();

            // 1. 加载用户自定义工具配置
            if (File.Exists(ConfigPath))
            {
                try
                {
                    var json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<ToolsConfig>(json);
                    if (config?.Tools != null)
                    {
                        tools.AddRange(config.Tools);
                    }
                }
                catch { }
            }

            // 2. 扫描工具文件夹
            if (Directory.Exists(ToolsDir))
            {
                ScanDirectory(ToolsDir, tools);
            }

            // 3. 缓存结果
            _cachedTools = tools;
            return tools;
        });
    }

    // 需要过滤的文件名关键词（非工具文件）
    private static readonly string[] _excludeKeywords = [
        "uninstall", "unins", "卸载", "update", "更新", "setup", "安装",
        "crash", "dump", "log", "tmp", "temp", "backup", "备份",
        "readme", "license", "说明", "help", "帮助"
    ];

    /// <summary>
    /// 递归扫描目录
    /// </summary>
    private static void ScanDirectory(string dir, List<ToolInfo> tools)
    {
        try
        {
            // 获取当前目录下的可执行文件
            foreach (var file in Directory.GetFiles(dir))
            {
                var ext = Path.GetExtension(file).ToLower();

                // 只扫描 exe 和 bat 文件
                if (ext is not (".exe" or ".bat" or ".cmd"))
                    continue;

                var fileName = Path.GetFileNameWithoutExtension(file).ToLower();

                // 过滤非工具文件
                if (_excludeKeywords.Any(k => fileName.Contains(k)))
                    continue;

                // 过滤小文件（可能是依赖库）
                var fileInfo = new FileInfo(file);
                if (fileInfo.Length < 10240) // 小于10KB
                    continue;

                var relativePath = Path.GetRelativePath(AppDomain.CurrentDomain.BaseDirectory, file);
                var category = GetCategoryFromPath(dir);
                var name = Path.GetFileNameWithoutExtension(file);

                // 检查是否已存在（避免重复）
                if (!tools.Any(t => t.RelativePath.Equals(relativePath, StringComparison.OrdinalIgnoreCase)))
                {
                    tools.Add(new ToolInfo(
                        Name: name,
                        Category: category,
                        Icon: " ",
                        Description: GetDescription(file),
                        RelativePath: relativePath
                    ));
                }
            }

            // 递归扫描子目录
            foreach (var subDir in Directory.GetDirectories(dir))
            {
                ScanDirectory(subDir, tools);
            }
        }
        catch { }
    }

    /// <summary>
    /// 从路径推断分类
    /// </summary>
    private static string GetCategoryFromPath(string dir)
    {
        var relativePath = Path.GetRelativePath(ToolsDir, dir);
        var parts = relativePath.Split(Path.DirectorySeparatorChar);

        // 第一级目录作为分类
        if (parts.Length > 0 && parts[0] != ".")
            return parts[0];

        return "未分类";
    }

    /// <summary>
    /// 获取工具描述（从exe文件版本信息）
    /// </summary>
    private static string GetDescription(string filePath)
    {
        try
        {
            var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(filePath);
            if (!string.IsNullOrEmpty(versionInfo.FileDescription))
                return versionInfo.FileDescription;
        }
        catch { }

        return Path.GetFileNameWithoutExtension(filePath);
    }

    /// <summary>
    /// 保存用户自定义工具配置
    /// </summary>
    public static void SaveConfig(List<ToolInfo> tools)
    {
        var config = new ToolsConfig { Tools = tools };
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }

    /// <summary>
    /// 添加工具到配置
    /// </summary>
    public static void AddTool(ToolInfo tool)
    {
        var tools = _cachedTools ?? new List<ToolInfo>();
        tools.Add(tool);
        _cachedTools = tools;
        SaveConfig(tools);
    }

    /// <summary>
    /// 删除工具
    /// </summary>
    public static void RemoveTool(ToolInfo tool)
    {
        if (_cachedTools == null) return;

        _cachedTools.RemoveAll(t => t.RelativePath == tool.RelativePath);
        SaveConfig(_cachedTools);
    }

    /// <summary>
    /// 清除缓存（强制重新扫描）
    /// </summary>
    public static void ClearCache()
    {
        _cachedTools = null;
    }

    /// <summary>
    /// 获取所有分类
    /// </summary>
    public static string[] GetCategories()
    {
        if (_cachedTools == null)
            return Array.Empty<string>();

        return _cachedTools
            .Select(t => t.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToArray();
    }
}

/// <summary>
/// 工具配置文件结构
/// </summary>
public class ToolsConfig
{
    public List<ToolInfo> Tools { get; set; } = new();
}
