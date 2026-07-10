using System.IO;
using System.Text.Json;
using 陈叔叔工具箱.Models;

namespace 陈叔叔工具箱.Helpers;

/// <summary>
/// 工具管理器 - 管理用户手动添加的工具
/// </summary>
public static class ToolScanner
{
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools.json");

    // 缓存的工具列表
    private static List<ToolInfo>? _cachedTools;

    /// <summary>
    /// 加载用户手动添加的工具
    /// </summary>
    public static Task<List<ToolInfo>> LoadUserToolsAsync()
    {
        return Task.Run(() =>
        {
            if (_cachedTools != null)
                return _cachedTools;

            _cachedTools = new List<ToolInfo>();

            // 加载用户自定义工具配置
            if (File.Exists(ConfigPath))
            {
                try
                {
                    var json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<ToolsConfig>(json);
                    if (config?.Tools != null)
                    {
                        _cachedTools.AddRange(config.Tools);
                    }
                }
                catch { }
            }

            return _cachedTools;
        });
    }

    /// <summary>
    /// 保存用户自定义工具配置
    /// </summary>
    public static void SaveConfig(List<ToolInfo> tools)
    {
        var config = new ToolsConfig { Tools = tools };
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
        _cachedTools = tools;
    }

    /// <summary>
    /// 添加工具到配置
    /// </summary>
    public static void AddTool(ToolInfo tool)
    {
        var tools = _cachedTools ?? new List<ToolInfo>();

        // 检查是否已存在
        if (!tools.Any(t => t.RelativePath == tool.RelativePath))
        {
            tools.Add(tool);
            SaveConfig(tools);
        }
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
    /// 清除缓存
    /// </summary>
    public static void ClearCache()
    {
        _cachedTools = null;
    }
}

/// <summary>
/// 工具配置文件结构
/// </summary>
public class ToolsConfig
{
    public List<ToolInfo> Tools { get; set; } = new();
}
