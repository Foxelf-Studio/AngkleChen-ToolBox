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
                result.IsError = true;
                result.ErrorMessage = "无法获取更新信息，请检查网络连接";
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
