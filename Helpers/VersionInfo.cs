using System.Text.Json.Serialization;

namespace 陈叔叔工具箱.Helpers;

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
/// 更新清单（manifest.json）
/// </summary>
public class UpdateManifest
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("releaseDate")]
    public string ReleaseDate { get; set; } = "";

    [JsonPropertyName("changelog")]
    public string Changelog { get; set; } = "";

    [JsonPropertyName("minUpgradeVersion")]
    public string MinUpgradeVersion { get; set; } = "";

    [JsonPropertyName("files")]
    public Dictionary<string, FileEntry> Files { get; set; } = new();
}

/// <summary>
/// 文件条目
/// </summary>
public class FileEntry
{
    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}

/// <summary>
/// 更新检查结果
/// </summary>
public class UpdateCheckResult
{
    public bool HasUpdate { get; set; }
    public string CurrentVersion { get; set; } = "";
    public string LatestVersion { get; set; } = "";
    public string Changelog { get; set; } = "";
    public string ReleaseUrl { get; set; } = "";
    public UpdateManifest? Manifest { get; set; }
    public List<ChangedFile> ChangedFiles { get; set; } = new();
}

/// <summary>
/// 需要更新的文件
/// </summary>
public class ChangedFile
{
    public string RelativePath { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public long Size { get; set; }
    public string ExpectedSha256 { get; set; } = "";
}
