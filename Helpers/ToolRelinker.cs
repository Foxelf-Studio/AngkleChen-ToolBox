using System.IO;

namespace 陈叔叔工具箱.Helpers;

/// <summary>一次成功的重新定位</summary>
/// <param name="MatchedBySize">true 表示文件名和体积都对上了；false 表示只靠文件名（体积未知）</param>
public sealed record RelinkHit(
    string ToolId,
    string ToolName,
    string CategoryName,
    string OldRelativePath,
    string NewRelativePath,
    bool MatchedBySize);

/// <summary>一次扫描的结果：命中的条目 + 因为候选不唯一而放弃的条目</summary>
public sealed record RelinkResult(List<RelinkHit> Hits, List<string> Ambiguous);

/// <summary>
/// 路径失效后的重新定位。
///
/// 场景：用户在资源管理器里把 <c>工具\某分类\某工具</c> 拖到了别处，
/// 配置里的 <c>relativePath</c> 就失效了，界面上这个工具会直接消失。
/// 这里在 <c>工具\</c> 下按「文件名 + 体积」找回来，唯一命中才改配置。
///
/// 三条硬性约束，宁可放弃也不猜：
/// · 候选必须唯一 —— 两个同名同体积的文件，认错哪个都是给用户制造麻烦
/// · 体积必须对得上（配置里记了的话）—— 同名但是新版本的 exe 不算同一个工具
/// · 不许抢别人的地址 —— 候选若已被其他工具登记，直接跳过
/// </summary>
public static class ToolRelinker
{
    public static RelinkResult FindRelinks(AppConfig config, string toolboxRoot)
    {
        var hits = new List<RelinkHit>();
        var ambiguous = new List<string>();

        var toolsRoot = Path.Combine(toolboxRoot, "工具");
        if (!Directory.Exists(toolsRoot)) return new RelinkResult(hits, ambiguous);

        // ── 1. 分出"还在的"和"丢了的" ────────────────
        var declared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var declaredDirs = new List<string>();   // 已登记的目录型工具，其内部文件不参与匹配
        var missing = new List<(AppConfig.CategoryConfig Category, AppConfig.ToolConfig Tool)>();

        foreach (var category in config.Categories)
        {
            foreach (var tool in category.Tools)
            {
                var relative = Normalize(tool.RelativePath);
                if (relative.Length == 0) continue;

                declared.Add(relative);

                var full = Path.Combine(toolboxRoot, relative);
                if (Directory.Exists(full)) declaredDirs.Add(relative + "\\");
                if (Directory.Exists(full) || File.Exists(full)) continue;

                missing.Add((category, tool));
            }
        }

        if (missing.Count == 0) return new RelinkResult(hits, ambiguous);

        // 别的工具目录内部的 exe（unins000、内置的 7z 之类）不是候选，
        // 否则一个叫 unins000.exe 的工具丢了，会被指到随便哪个工具的卸载程序上。
        bool InsideOtherTool(string relative) =>
            declaredDirs.Any(d => relative.StartsWith(d, StringComparison.OrdinalIgnoreCase));

        // ── 2. 给 工具\ 建索引 ────────────────────────
        var filesByName = new Dictionary<string, List<(string Relative, long Size)>>(StringComparer.OrdinalIgnoreCase);
        var dirsByName = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var file in Directory.EnumerateFiles(toolsRoot, "*", SearchOption.AllDirectories))
            {
                var relative = Normalize(Path.GetRelativePath(toolboxRoot, file));
                if (declared.Contains(relative)) continue;   // 已经有主了
                if (InsideOtherTool(relative)) continue;     // 在别人的工具目录里

                long size;
                try { size = new FileInfo(file).Length; }
                catch { continue; }

                var name = Path.GetFileName(file);
                if (!filesByName.TryGetValue(name, out var bucket))
                    filesByName[name] = bucket = new List<(string, long)>();

                bucket.Add((relative, size));
            }

            foreach (var dir in Directory.EnumerateDirectories(toolsRoot, "*", SearchOption.AllDirectories))
            {
                var relative = Normalize(Path.GetRelativePath(toolboxRoot, dir));
                if (declared.Contains(relative)) continue;

                var name = Path.GetFileName(dir);
                if (!dirsByName.TryGetValue(name, out var bucket))
                    dirsByName[name] = bucket = new List<string>();

                bucket.Add(relative);
            }
        }
        catch
        {
            // 枚举失败就放弃本次修复，不动配置
            return new RelinkResult(hits, ambiguous);
        }

        // ── 3. 逐个匹配，同一个候选只能被认领一次 ──────
        var claimed = new HashSet<string>(declared, StringComparer.OrdinalIgnoreCase);

        foreach (var (category, tool) in missing
                     .OrderBy(m => m.Category.Name, StringComparer.CurrentCulture)
                     .ThenBy(m => m.Tool.Name, StringComparer.CurrentCulture))
        {
            var oldRelative = Normalize(tool.RelativePath);
            var name = Path.GetFileName(oldRelative);
            if (name.Length == 0) continue;

            // 目录型工具（路径没有扩展名）按名字找目录
            if (!Path.HasExtension(name))
            {
                if (!dirsByName.TryGetValue(name, out var dirs)) continue;

                var free = dirs.Where(d => !claimed.Contains(d)).ToList();
                if (free.Count == 0) continue;

                if (free.Count > 1)
                {
                    ambiguous.Add($"{tool.Name}（{free.Count} 个同名目录）");
                    continue;
                }

                claimed.Add(free[0]);
                hits.Add(new RelinkHit(tool.Id, tool.Name, category.Name, oldRelative, free[0], false));
                continue;
            }

            if (!filesByName.TryGetValue(name, out var candidates)) continue;

            var pool = candidates.Where(c => !claimed.Contains(c.Relative)).ToList();
            if (pool.Count == 0) continue;

            if (tool.Size > 0)
            {
                var sameSize = pool.Where(c => c.Size == tool.Size).ToList();

                if (sameSize.Count == 1)
                {
                    claimed.Add(sameSize[0].Relative);
                    hits.Add(new RelinkHit(tool.Id, tool.Name, category.Name,
                        oldRelative, sameSize[0].Relative, true));
                }
                else if (sameSize.Count > 1)
                {
                    ambiguous.Add($"{tool.Name}（{sameSize.Count} 个同名同体积文件）");
                }
                // 体积全对不上 → 大概率是被换成了别的版本，不当成同一个工具

                continue;
            }

            // 体积未知（旧配置且启动时文件已丢失）：只接受唯一同名
            if (pool.Count == 1)
            {
                claimed.Add(pool[0].Relative);
                hits.Add(new RelinkHit(tool.Id, tool.Name, category.Name,
                    oldRelative, pool[0].Relative, false));
            }
            else
            {
                ambiguous.Add($"{tool.Name}（{pool.Count} 个同名文件，且体积未知）");
            }
        }

        return new RelinkResult(hits, ambiguous);
    }

    private static string Normalize(string path)
        => path.Replace('/', '\\').TrimStart('\\');
}
