using System.Text.Json.Serialization;

namespace 陈叔叔工具箱.Helpers;

/// <summary>
/// 版本号统一出口（版本号散落在多处写死默认值是常见事故源）
/// </summary>
public static class VersionInfo
{
    /// <summary>当前程序版本，形如 2.1.0</summary>
    public static string Current { get; } =
        typeof(VersionInfo).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
}

/// <summary>
/// GitHub Release 信息
/// </summary>
public class ReleaseInfo
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("body")]
    public string Body { get; set; } = "";

    [JsonPropertyName("published_at")]
    public string PublishedAt { get; set; } = "";

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = "";

    [JsonPropertyName("assets")]
    public List<ReleaseAsset> Assets { get; set; } = new();
}

/// <summary>
/// GitHub Release 资源文件
/// </summary>
public class ReleaseAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }
}


/// <summary>
/// 更新检查结果
/// </summary>
public class UpdateCheckResult
{
    public bool HasUpdate { get; set; }
    public bool IsError { get; set; }
    public string ErrorMessage { get; set; } = "";
    public string CurrentVersion { get; set; } = "";
    public string LatestVersion { get; set; } = "";
    public string Changelog { get; set; } = "";
    public string ReleaseUrl { get; set; } = "";
}
