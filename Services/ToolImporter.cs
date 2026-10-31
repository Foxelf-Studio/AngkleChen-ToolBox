using System.IO;
using 陈叔叔工具箱.Helpers;

namespace 陈叔叔工具箱.Services;

/// <summary>
/// 工具导入：把用户选的 exe / 文件夹拷贝进 工具\&lt;分类&gt;\，再写进 config.json。
/// 从 MainWindow 抽出，窗口只负责弹对话框、取返回值。
/// </summary>
public static class ToolImporter
{
    public record ImportResult(bool Success, string Message, string? RelativePath = null);

    /// <summary>
    /// 导入一个工具
    /// </summary>
    /// <param name="sourcePath">用户选中的文件或目录（绝对路径）</param>
    /// <param name="categoryName">目标分类</param>
    public static ImportResult Import(
        AppConfig config,
        string toolboxRoot,
        string sourcePath,
        string categoryName,
        string toolName,
        string description,
        string? categoryIcon = null)
    {
        try
        {
            var isFolder = Directory.Exists(sourcePath);
            var sourceName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrEmpty(sourceName))
                return new ImportResult(false, "无法识别所选路径的名称。");

            var destDir = Path.Combine(toolboxRoot, "工具", categoryName);
            var destPath = Path.Combine(destDir, sourceName);

            // 已经位于 工具\ 下就不要再拷一遍（用全路径比对，避免"工具A"误匹配"工具A2"）
            var toolsRoot = Path.Combine(toolboxRoot, "工具");
            var normalized = Path.GetFullPath(sourcePath);
            var alreadyInside = normalized.StartsWith(
                toolsRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase);

            if (!alreadyInside)
            {
                Directory.CreateDirectory(destDir);

                if (isFolder)
                    CopyDirectory(sourcePath, destPath);
                else
                    File.Copy(sourcePath, destPath, true);

                Logger.Log($"已复制到工具目录: {sourcePath} -> {destPath}");
            }
            else
            {
                Logger.Log($"源已在工具目录内，跳过复制: {sourcePath}");
            }

            var relativePath = Path.GetRelativePath(toolboxRoot, destPath);
            var added = config.AddTool(categoryName, new AppConfig.ToolConfig
            {
                Name = toolName,
                Description = description,
                RelativePath = relativePath
            }, categoryIcon);

            return added
                ? new ImportResult(true, $"已添加工具：{toolName}", relativePath)
                : new ImportResult(false, $"「{toolName}」已存在于该分类中。", relativePath);
        }
        catch (Exception ex)
        {
            Logger.Log($"导入工具失败: {ex.Message}");
            return new ImportResult(false, $"添加工具失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 递归复制目录（只拷文件，跳过空目录的额外判断）
    /// </summary>
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }
    }
}
