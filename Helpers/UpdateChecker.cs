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
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)  // 10秒超时，避免卡死
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ChenUncle-Toolbox");
    }

    /// <summary>
    /// 检查是否有新版本
    /// </summary>
    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        Logger.Log("=== 开始检查更新 ===");

        var result = new UpdateCheckResult
        {
            CurrentVersion = GetCurrentVersion()
        };
        Logger.Log($"当前版本: {result.CurrentVersion}");

        try
        {
            // 获取最新 Release
            Logger.Log("正在获取最新 Release...");
            var release = await GetLatestReleaseAsync();

            if (release == null)
            {
                Logger.Log("获取 Release 失败，返回 null");
                return result;
            }

            Logger.Log($"获取到 Release: tag={release.TagName}, name={release.Name}");

            result.LatestVersion = NormalizeVersion(release.TagName);
            result.Changelog = release.Body;
            result.ReleaseUrl = release.HtmlUrl;

            Logger.Log($"最新版本: {result.LatestVersion}");

            // 比较版本
            var compareResult = CompareVersions(result.CurrentVersion, result.LatestVersion);
            Logger.Log($"版本比较结果: {compareResult} (当前={result.CurrentVersion}, 最新={result.LatestVersion})");

            if (compareResult >= 0)
            {
                Logger.Log("当前已是最新版本");
                return result;
            }

            result.HasUpdate = true;
            Logger.Log("发现新版本！");
        }
        catch (Exception ex)
        {
            Logger.Log($"检查更新异常: {ex.Message}");
            Logger.Log($"异常堆栈: {ex.StackTrace}");
            result.HasUpdate = false;
        }

        Logger.Log($"=== 检查更新完成: HasUpdate={result.HasUpdate} ===");
        return result;
    }

    /// <summary>
    /// 获取最新 Release 信息（包含 pre-release）
    /// </summary>
    private async Task<ReleaseInfo?> GetLatestReleaseAsync()
    {
        // 使用 /releases 而不是 /releases/latest，因为后者不返回 pre-release
        var url = $"https://api.github.com/repos/{_owner}/{_repo}/releases?per_page=1";
        Logger.Log($"请求 URL: {url}");

        try
        {
            Logger.Log("发送 HTTP 请求...");
            var response = await _httpClient.GetAsync(url);
            Logger.Log($"HTTP 响应状态: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                Logger.Log($"请求失败: {response.StatusCode}");
                return null;
            }

            Logger.Log("读取响应内容...");
            var json = await response.Content.ReadAsStringAsync();
            Logger.Log($"响应内容长度: {json.Length} 字节");

            Logger.Log("解析 JSON...");
            var releases = JsonSerializer.Deserialize<List<ReleaseInfo>>(json, JsonOptions);

            if (releases == null || releases.Count == 0)
            {
                Logger.Log("解析结果为空");
                return null;
            }

            Logger.Log($"解析成功，共 {releases.Count} 个 Release");
            return releases.FirstOrDefault();
        }
        catch (TaskCanceledException)
        {
            Logger.Log("请求超时");
            return null;
        }
        catch (HttpRequestException ex)
        {
            Logger.Log($"网络请求异常: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Log($"未知异常: {ex.Message}");
            return null;
        }
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

        // Lite 版用户没有扩展工具目录，跳过扩展工具文件
        var hasExtDir = Directory.Exists(Path.Combine(baseDir, "扩展工具"));

        foreach (var (relativePath, entry) in manifest.Files)
        {
            var localPath = Path.Combine(baseDir, relativePath);

            // Lite 版跳过扩展工具文件
            if (!hasExtDir && relativePath.StartsWith("扩展工具", StringComparison.OrdinalIgnoreCase))
                continue;

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
