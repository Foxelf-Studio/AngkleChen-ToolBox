using System.IO;
using System.Text.Json;

namespace 陈叔叔工具箱.Helpers;

/// <summary>
/// settings.json 的唯一读写入口。
///
/// 此前 WPF 的设置面板直接内联拼字典 JSON，换框架时必然写出第二份。
/// 这里收敛成一处，UI 只负责把开关状态交给它。
/// </summary>
public sealed class AppSettings
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    /// <summary>启动后自动检查更新</summary>
    public bool AutoCheckUpdate { get; set; } = true;

    /// <summary>写 debug.log。默认关闭，排查问题时才开</summary>
    public bool EnableLog { get; set; }

    /// <summary>界面主题：system / light / dark</summary>
    public string Theme { get; set; } = "system";

    public static AppSettings Load()
    {
        var settings = new AppSettings();

        try
        {
            if (!File.Exists(AppPaths.Settings)) return settings;

            var json = File.ReadAllText(AppPaths.Settings);

            // 用 JsonElement 而不是强类型：旧文件的字段是 bool，
            // 新加的 theme 是字符串，同一个字典里两种类型都要能读
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (values == null) return settings;

            if (values.TryGetValue("autoCheckUpdate", out var autoCheck) &&
                autoCheck.ValueKind is JsonValueKind.True or JsonValueKind.False)
                settings.AutoCheckUpdate = autoCheck.GetBoolean();

            if (values.TryGetValue("enableLog", out var enableLog) &&
                enableLog.ValueKind is JsonValueKind.True or JsonValueKind.False)
                settings.EnableLog = enableLog.GetBoolean();

            if (values.TryGetValue("theme", out var theme) && theme.ValueKind == JsonValueKind.String)
                settings.Theme = theme.GetString() ?? "system";
        }
        catch
        {
            // 读不出来就用默认值，不让一个坏掉的设置文件挡住启动
        }

        return settings;
    }

    public void Save()
    {
        try
        {
            var values = new Dictionary<string, object>
            {
                ["autoCheckUpdate"] = AutoCheckUpdate,
                ["enableLog"] = EnableLog,
                ["theme"] = Theme
            };

            File.WriteAllText(AppPaths.Settings, JsonSerializer.Serialize(values, WriteOptions));
        }
        catch
        {
            // 写不进去不影响运行
        }
    }
}
