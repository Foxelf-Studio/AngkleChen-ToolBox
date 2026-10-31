using System.IO;

namespace 陈叔叔工具箱.Helpers;

/// <summary>扫描到的一个未收录条目</summary>
public sealed record ScanCandidate(string CategoryName, string ToolName, string RelativePath);

/// <summary>
/// 扫描 <c>工具\</c> 目录，找出配置里没有登记的条目。
/// 只负责"发现"，收不收由用户决定——绝不自动改配置。
/// </summary>
public static class ToolScanner
{
    private static readonly string[] ExecutableExtensions = [".exe", ".bat", ".cmd"];

    /// <summary>
    /// 找出 工具\&lt;分类&gt;\ 下所有未登记的候选工具。
    ///
    /// 判定规则（刻意保守，宁可漏报也不要刷出一堆运行文件）：
    /// · 子目录：只要里面有可执行文件，且未被任何已登记路径覆盖 → 候选
    /// · 散装文件：仅当该分类顶层还没有"散装 exe 型"工具时才当候选
    ///   （否则 FurMark 目录里的 DLL、txt、csv 会全被当成工具）
    /// </summary>
    public static List<ScanCandidate> ScanUnregistered(AppConfig config, string toolboxRoot)
    {
        var result = new List<ScanCandidate>();
        var toolsRoot = Path.Combine(toolboxRoot, "工具");
        if (!Directory.Exists(toolsRoot)) return result;

        var registered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in config.Categories)
            foreach (var tool in category.Tools)
                registered.Add(Normalize(tool.RelativePath));

        foreach (var categoryDir in Directory.GetDirectories(toolsRoot))
        {
            var categoryName = Path.GetFileName(categoryDir);
            if (string.IsNullOrEmpty(categoryName)) continue;

            var categoryRelative = Normalize(Path.Combine("工具", categoryName));
            var hasLooseFileTools = registered.Any(r =>
                string.Equals(Path.GetDirectoryName(r), categoryRelative, StringComparison.OrdinalIgnoreCase));

            foreach (var entry in Directory.GetFileSystemEntries(categoryDir))
            {
                var entryName = Path.GetFileName(entry);
                var relativePath = Normalize(Path.Combine("工具", categoryName, entryName));

                if (IsCovered(registered, relativePath)) continue;

                if (Directory.Exists(entry))
                {
                    var mainExe = FindMainExecutable(entry);
                    if (mainExe == null) continue;

                    result.Add(new ScanCandidate(
                        categoryName,
                        entryName,
                        Normalize(Path.Combine(relativePath, Path.GetFileName(mainExe)))));
                }
                else
                {
                    if (hasLooseFileTools) continue;
                    if (!IsExecutable(entry)) continue;

                    result.Add(new ScanCandidate(categoryName, Path.GetFileNameWithoutExtension(entry), relativePath));
                }
            }
        }

        return result
            .OrderBy(c => c.CategoryName, StringComparer.CurrentCulture)
            .ThenBy(c => c.ToolName, StringComparer.CurrentCulture)
            .ToList();
    }

    /// <summary>该路径本身已登记，或位于某个已登记的目录之下</summary>
    private static bool IsCovered(HashSet<string> registered, string relativePath)
        => registered.Any(r =>
            string.Equals(r, relativePath, StringComparison.OrdinalIgnoreCase) ||
            r.StartsWith(relativePath + "\\", StringComparison.OrdinalIgnoreCase));

    private static bool IsExecutable(string path)
        => ExecutableExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>目录里最像"主程序"的文件：先找与目录同名的，否则取体积最大的</summary>
    private static string? FindMainExecutable(string directory)
    {
        try
        {
            var candidates = Directory
                .GetFiles(directory, "*", SearchOption.TopDirectoryOnly)
                .Where(IsExecutable)
                .ToList();

            if (candidates.Count == 0) return null;

            var folderName = Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar));
            var sameName = candidates.FirstOrDefault(f =>
                string.Equals(Path.GetFileNameWithoutExtension(f), folderName, StringComparison.OrdinalIgnoreCase));

            return sameName ?? candidates.OrderByDescending(f => new FileInfo(f).Length).First();
        }
        catch
        {
            return null;
        }
    }

    private static string Normalize(string path)
        => path.Replace('/', '\\').TrimStart('\\');
}
