using System.Diagnostics;
using System.IO;
using 陈叔叔工具箱.Helpers;
using 陈叔叔工具箱.Models;

namespace 陈叔叔工具箱.Services;

/// <summary>
/// 工具启动器：把"双击卡片"变成一次进程启动。
/// 从 MainWindow 抽出来，窗口只负责提示结果，不关心 .bat / .msi / 目录的差别。
/// </summary>
public static class ToolLauncher
{
    /// <summary>
    /// 启动工具。成功返回 null，失败返回可直接展示给用户的错误信息。
    /// </summary>
    public static string? Launch(string toolboxRoot, ToolInfo tool)
    {
        var path = Path.Combine(toolboxRoot, tool.RelativePath);

        if (!File.Exists(path) && !Directory.Exists(path))
            return $"文件不存在：\n{path}";

        try
        {
            // 目录直接开资源管理器
            if (Directory.Exists(path))
            {
                Process.Start("explorer.exe", $"\"{path}\"");
                return null;
            }

            var extension = Path.GetExtension(path).ToLowerInvariant();
            var workDir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(workDir)) workDir = toolboxRoot;

            var psi = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = workDir
            };

            // .bat/.cmd 必须经由 cmd，直接在 UseShellExecute 下启动会闪退
            if (extension is ".bat" or ".cmd")
            {
                psi.FileName = "cmd.exe";
                psi.Arguments = $"/c \"{path}\"";
            }
            else
            {
                psi.FileName = path;
            }

            Process.Start(psi);
            return null;
        }
        catch (Exception ex)
        {
            Logger.Log($"启动失败: {tool.Name} - {ex.Message}");
            return $"启动失败：\n{ex.Message}";
        }
    }
}
