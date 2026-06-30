using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace 陈叔叔工具箱.Helpers;

/// <summary>
/// 更新检查器 - 通过 GitHub Release 检查和下载更新
/// </summary>
public class UpdateChecker
{
    private readonly string _owner;
    private readonly string _repo;
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public UpdateChecker(string owner, string repo)
    {
        _owner = owner;
        _repo = repo;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ChenUncle-Toolbox");
    }

    /// <summary>
    /// 检查是否有新版本
    /// </summary>
    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        var result = new UpdateCheckResult
        {
            CurrentVersion = GetCurrentVersion()
        };

        try
        {
            // 获取最新 Release
            var release = await GetLatestReleaseAsync();
            if (release == null)
                return result;

            result.LatestVersion = NormalizeVersion(release.TagName);
            result.Changelog = release.Body;
            result.ReleaseUrl = release.HtmlUrl;

            // 比较版本
            if (CompareVersions(result.CurrentVersion, result.LatestVersion) >= 0)
                return result;

            result.HasUpdate = true;

            // 尝试获取 manifest.json（用于增量更新）
            var manifestAsset = release.Assets.FirstOrDefault(a => a.Name == "manifest.json");
            if (manifestAsset != null)
            {
                result.Manifest = await GetManifestAsync(manifestAsset.BrowserDownloadUrl);
                if (result.Manifest != null)
                {
                    result.ChangedFiles = GetChangedFiles(result.Manifest);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"检查更新失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 获取最新 Release 信息
    /// </summary>
    private async Task<ReleaseInfo?> GetLatestReleaseAsync()
    {
        var url = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ReleaseInfo>(json, JsonOptions);
    }

    /// <summary>
    /// 获取更新清单
    /// </summary>
    private async Task<UpdateManifest?> GetManifestAsync(string manifestUrl)
    {
        try
        {
            var response = await _httpClient.GetAsync(manifestUrl);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UpdateManifest>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 对比本地文件，获取需要更新的文件列表
    /// </summary>
    private List<ChangedFile> GetChangedFiles(UpdateManifest manifest)
    {
        var changedFiles = new List<ChangedFile>();
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        foreach (var (relativePath, entry) in manifest.Files)
        {
            var localPath = Path.Combine(baseDir, relativePath);

            // 文件不存在或 SHA256 不匹配
            if (!File.Exists(localPath) || CalculateFileSha256(localPath) != entry.Sha256)
            {
                changedFiles.Add(new ChangedFile
                {
                    RelativePath = relativePath,
                    DownloadUrl = entry.Url,
                    Size = entry.Size,
                    ExpectedSha256 = entry.Sha256
                });
            }
        }

        return changedFiles;
    }

    /// <summary>
    /// 计算文件 SHA256
    /// </summary>
    public static string CalculateFileSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// 下载单个文件
    /// </summary>
    public async Task<bool> DownloadFileAsync(string url, string destPath, IProgress<double>? progress = null)
    {
        try
        {
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
                return false;

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            var tempPath = destPath + ".tmp";

            // 确保目录存在
            var dir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using (var contentStream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[81920];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalRead += bytesRead;

                    if (totalBytes > 0)
                        progress?.Report((double)totalRead / totalBytes * 100);
                }
            }

            // 替换原文件
            if (File.Exists(destPath))
                File.Delete(destPath);
            File.Move(tempPath, destPath);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"下载文件失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取当前版本号
    /// </summary>
    private static string GetCurrentVersion()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        return version?.ToString(3) ?? "1.0.0";
    }

    /// <summary>
    /// 标准化版本号（移除 v 前缀）
    /// </summary>
    private static string NormalizeVersion(string version)
    {
        if (string.IsNullOrEmpty(version))
            return "0.0.0";

        version = version.Trim();
        if (version.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            version = version[1..];

        // 确保至少有三段
        var parts = version.Split('.');
        if (parts.Length < 3)
            version = string.Join(".", parts) + string.Concat(Enumerable.Repeat(".0", 3 - parts.Length));

        return version;
    }

    /// <summary>
    /// 比较版本号
    /// </summary>
    /// <returns>大于0表示v1>v2，等于0表示相同，小于0表示v1<v2</returns>
    private static int CompareVersions(string v1, string v2)
    {
        try
        {
            var version1 = new Version(v1);
            var version2 = new Version(v2);
            return version1.CompareTo(version2);
        }
        catch
        {
            return string.Compare(v1, v2, StringComparison.OrdinalIgnoreCase);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
