using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using 陈叔叔工具箱.Models;

namespace 陈叔叔工具箱.Helpers;

/// <summary>
/// 应用程序配置管理器 - 统一管理工具和分类
/// </summary>
public class AppConfig
{
    // 路径统一由 AppPaths 提供，避免多处各自计算
    private static string BaseDir => AppPaths.Root;
    private static string ConfigPath => AppPaths.Config;
    private static string IconCacheDir => AppPaths.IconCache;
    private static string TrashDir => AppPaths.Trash;
    private static readonly object _lock = new();

    /// <summary>新建分类时的默认图标（Segoe MDL2：文件夹）</summary>
    public const string DefaultCategoryIcon = "\uE8B7";

    /// <summary>
    /// 配置数据
    /// </summary>
    [JsonPropertyName("categories")]
    public List<CategoryConfig> Categories { get; set; } = new();

    /// <summary>
    /// 回收站（删除的工具/分类暂存于此，可恢复）
    /// </summary>
    [JsonPropertyName("trash")]
    public List<TrashEntry> Trash { get; set; } = new();

    /// <summary>
    /// 分类配置
    /// </summary>
    public class CategoryConfig
    {
        /// <summary>稳定标识。改名字、搬目录都不会变，避免用名称当主键</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = DefaultCategoryIcon;

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("tools")]
        public List<ToolConfig> Tools { get; set; } = new();
    }

    /// <summary>
    /// 工具配置
    /// </summary>
    public class ToolConfig
    {
        /// <summary>稳定标识。改名称、改路径都不会变</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("relativePath")]
        public string RelativePath { get; set; } = "";

        /// <summary>分类内排序权重，越小越靠前；0 表示从未手动排序（按名称排）</summary>
        [JsonPropertyName("order")]
        public int Order { get; set; }

        /// <summary>
        /// 最近一次确认存在时的文件大小（字节）。0 表示未知——目录型工具恒为 0。
        ///
        /// 用途：文件被搬走后，靠「文件名 + 体积」在 工具\ 里重新定位它。
        /// 没有这个字段就只能按名字猜，同名文件一多就会认错。
        /// </summary>
        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    /// <summary>
    /// 回收站条目
    /// </summary>
    public class TrashEntry
    {
        /// <summary>回收站条目自身的 id（也是 回收站\\<id> 目录名）</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        /// <summary>原工具的 id，恢复时沿用，保证身份不变</summary>
        [JsonPropertyName("toolId")]
        public string ToolId { get; set; } = "";

        /// <summary>原分类的 id，恢复时优先按它找回分类</summary>
        [JsonPropertyName("categoryId")]
        public string CategoryId { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("relativePath")]
        public string RelativePath { get; set; } = "";

        [JsonPropertyName("category")]
        public string Category { get; set; } = "";

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = "";

        /// <summary>true 表示删除的是整个分类</summary>
        [JsonPropertyName("isCategory")]
        public bool IsCategory { get; set; }

        [JsonPropertyName("deletedAt")]
        public DateTime DeletedAt { get; set; } = DateTime.Now;

        /// <summary>分类级删除时，暂存该分类下所有工具配置</summary>
        [JsonPropertyName("tools")]
        public List<ToolConfig>? Tools { get; set; }
    }

    /// <summary>
    /// 本次启动是否因配置文件缺失/损坏而用内置模板重建过
    /// </summary>
    [JsonIgnore]
    public bool RestoredFromTemplate { get; private set; }

    /// <summary>
    /// 配置损坏时备份出来的文件名（形如 config.json.broken-20261024-221500）
    /// </summary>
    [JsonIgnore]
    public string? BrokenBackupFile { get; private set; }

    /// <summary>
    /// 加载配置。config.json 是工具清单的唯一来源。
    ///
    /// 三种情况分开处理，避免误伤用户的自定义数据：
    /// · 文件不存在 → 从内置模板重建（首次运行 / 被误删）
    /// · 解析失败   → 先把坏文件备份一份，再重建（用户手改出错也能找回）
    /// · 解析成功但清单为空 → 尊重用户意图，直接返回空清单，绝不擅自"恢复出厂"
    /// </summary>
    public static AppConfig Load()
    {
        lock (_lock)
        {
            string? brokenBackup = null;

            if (File.Exists(ConfigPath))
            {
                try
                {
                    var json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json);

                    if (config != null)
                    {
                        // 修复绝对路径为相对路径
                        config.FixAbsolutePaths();

                        // schema v1 → v2：给老配置补 id（幂等，补过就不再动）
                        var schemaChanged = config.EnsureIds();

                        // 记录体积，供"路径失效后重新定位"比对（只在真的变了时写盘）
                        schemaChanged |= config.SyncSizes();

                        if (schemaChanged)
                        {
                            config.Save();
                            Logger.Log("已为旧配置补齐 id / 体积信息（schema v2）");
                        }

                        if (config.Categories.Count == 0)
                            Logger.Log("配置里没有任何分类（可能是用户主动清空），按空清单启动");

                        Logger.Log($"加载配置: {config.Categories.Count} 个分类, {config.GetAllTools().Count} 个工具");
                        return config;
                    }

                    Logger.Log("配置内容为 null，改用内置模板");
                    brokenBackup = BackupBrokenConfig("内容为 null");
                }
                catch (Exception ex)
                {
                    Logger.Log($"加载配置失败: {ex.Message}");
                    brokenBackup = BackupBrokenConfig(ex.Message);
                }
            }

            var rebuilt = LoadEmbeddedTemplate();
            if (rebuilt != null)
            {
                rebuilt.RestoredFromTemplate = true;
                rebuilt.BrokenBackupFile = brokenBackup;

                // 内置模板可能还是 v1 格式（仓库里的 config.json 不带 id），
                // 不补 id 的话所有按 id 的操作（编辑/排序/删除/重命名）都会失效，
                // 更糟的是空 id 会匹配到列表里第一个分类，导致改错对象。
                rebuilt.EnsureIds();
                rebuilt.SyncSizes();   // 工具目录还在的话，顺手把体积也记上
                rebuilt.Save();
                Logger.Log($"已从内置模板重建配置: {rebuilt.Categories.Count} 个分类, {rebuilt.GetAllTools().Count} 个工具");
                return rebuilt;
            }

            // 连内置模板都读不出来（理论上不会发生）：返回空配置，界面显示 0 个工具，而不是编造数据
            Logger.Log("内置模板不可用，返回空配置");
            return new AppConfig { BrokenBackupFile = brokenBackup };
        }
    }

    /// <summary>
    /// 把无法解析的 config.json 复制一份带时间戳的副本，避免用户手改出错时数据直接蒸发
    /// </summary>
    private static string? BackupBrokenConfig(string reason)
    {
        try
        {
            var name = $"config.json.broken-{DateTime.Now:yyyyMMdd-HHmmss}";
            var target = Path.Combine(BaseDir, name);

            // 同一秒内重复触发时避免覆盖
            var index = 1;
            while (File.Exists(target))
                target = Path.Combine(BaseDir, $"{name}-{index++}");

            File.Copy(ConfigPath, target, true);
            Logger.Log($"配置无法解析（{reason}），已备份为 {Path.GetFileName(target)}");
            return Path.GetFileName(target);
        }
        catch (Exception ex)
        {
            Logger.Log($"备份损坏配置失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 读取编译时嵌入程序集的 config.json 模板
    /// </summary>
    private static AppConfig? LoadEmbeddedTemplate()
    {
        try
        {
            using var stream = typeof(AppConfig).Assembly.GetManifestResourceStream("config.json");
            if (stream == null)
            {
                Logger.Log("未找到内置模板资源 config.json");
                return null;
            }

            using var reader = new StreamReader(stream);
            return JsonSerializer.Deserialize<AppConfig>(reader.ReadToEnd());
        }
        catch (Exception ex)
        {
            Logger.Log($"读取内置模板失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 修复配置中的绝对路径为相对路径
    /// </summary>
    private void FixAbsolutePaths()
    {
        var baseDir = BaseDir;
        var fixedCount = 0;

        foreach (var category in Categories)
        {
            foreach (var tool in category.Tools)
            {
                if (Path.IsPathRooted(tool.RelativePath))
                {
                    try
                    {
                        tool.RelativePath = Path.GetRelativePath(baseDir, tool.RelativePath);
                        fixedCount++;
                        Logger.Log($"修复路径: {tool.Name} -> {tool.RelativePath}");
                    }
                    catch { }
                }
            }
        }

        if (fixedCount > 0)
        {
            Save();
            Logger.Log($"修复了 {fixedCount} 个绝对路径");
        }
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    public void Save()
    {
        lock (_lock)
        {
            try
            {
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

                // 原子写入：先写临时文件再替换，避免写到一半断电把唯一配置写坏
                var tmp = ConfigPath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(ConfigPath))
                    File.Replace(tmp, ConfigPath, ConfigPath + ".bak");
                else
                    File.Move(tmp, ConfigPath);

                Logger.Log($"保存配置: {Categories.Count} 个分类, {Trash.Count} 个回收站条目");
            }
            catch (Exception ex)
            {
                Logger.Log($"保存配置失败: {ex.Message}");
            }
        }
    }

    /// <summary>生成短 id（12 位十六进制），足够避免碰撞且不臃肿</summary>
    private static string NewId() => Guid.NewGuid().ToString("N")[..12];

    /// <summary>
    /// 给缺失 id 的分类/工具补齐。schema v1 → v2 的迁移入口，幂等。
    /// </summary>
    private bool EnsureIds()
    {
        var changed = false;

        foreach (var category in Categories)
        {
            if (string.IsNullOrEmpty(category.Id))
            {
                category.Id = NewId();
                changed = true;
            }

            foreach (var tool in category.Tools)
            {
                if (string.IsNullOrEmpty(tool.Id))
                {
                    tool.Id = NewId();
                    changed = true;
                }
            }
        }

        return changed;
    }

    /// <summary>
    /// 把每个工具的当前体积记进配置，供「路径失效后重新定位」比对。
    ///
    /// 只在文件存在且体积与记录不一致时才改——文件被换成新版本时这里会更新，
    /// 文件已经不见了则保持旧值不动（那正是重新定位要用来比对的基准）。
    /// </summary>
    private bool SyncSizes()
    {
        var changed = false;

        foreach (var category in Categories)
        {
            foreach (var tool in category.Tools)
            {
                if (string.IsNullOrEmpty(tool.RelativePath)) continue;

                try
                {
                    var full = Path.Combine(BaseDir, tool.RelativePath);
                    if (!File.Exists(full)) continue;   // 目录型工具恒为 0，不参与体积比对

                    var length = new FileInfo(full).Length;
                    if (length == tool.Size) continue;

                    tool.Size = length;
                    changed = true;
                }
                catch
                {
                    // 单个文件读不到不影响整体
                }
            }
        }

        return changed;
    }

    /// <summary>
    /// 把工具指向新的相对路径（文件被用户搬到 工具\ 下别处时使用）。
    /// 目标必须真实存在，且不能已经属于另一个工具——否则会产生两条指向同一文件的记录。
    /// </summary>
    public bool RelinkTool(string toolId, string newRelativePath)
    {
        lock (_lock)
        {
            var found = FindTool(toolId);
            if (found == null) return false;

            var tool = found.Value.Tool;

            newRelativePath = (newRelativePath ?? "").Replace('/', '\\').TrimStart('\\');
            if (newRelativePath.Length == 0) return false;

            if (tool.RelativePath.Equals(newRelativePath, StringComparison.OrdinalIgnoreCase))
                return false;

            // 目标已被别的工具占用 → 放弃，避免两条记录共用一个文件
            var taken = Categories
                .SelectMany(c => c.Tools)
                .Any(t => !ReferenceEquals(t, tool) &&
                          t.RelativePath.Equals(newRelativePath, StringComparison.OrdinalIgnoreCase));

            if (taken)
            {
                Logger.Log($"重新定位跳过：{newRelativePath} 已被其他工具登记");
                return false;
            }

            var full = Path.Combine(BaseDir, newRelativePath);
            var isFile = File.Exists(full);
            if (!isFile && !Directory.Exists(full)) return false;

            var oldPath = tool.RelativePath;
            tool.RelativePath = newRelativePath;

            try
            {
                if (isFile) tool.Size = new FileInfo(full).Length;
            }
            catch
            {
                // 体积读不到就保持旧值
            }

            Save();
            MoveCachedIcon(oldPath, newRelativePath);

            Logger.Log($"重新定位: {tool.Name}  {oldPath}  ->  {newRelativePath}");
            return true;
        }
    }

    /// <summary>按 id 查找工具及其所属分类</summary>
    private (CategoryConfig Category, ToolConfig Tool)? FindTool(string toolId)
    {
        if (string.IsNullOrEmpty(toolId)) return null;

        foreach (var category in Categories)
        {
            var tool = category.Tools.FirstOrDefault(t => t.Id == toolId);
            if (tool != null) return (category, tool);
        }

        return null;
    }

    /// <summary>
    /// 按 id 查找分类。
    ///
    /// 必须先挡掉空 id：<c>c.Id == ""</c> 会命中列表里第一个「没有 id 的分类」，
    /// 那样重命名 / 删除就会作用到用户没点的那个分类上——改错文件夹比操作失败严重得多。
    /// </summary>
    private CategoryConfig? FindCategoryById(string? categoryId)
    {
        if (string.IsNullOrEmpty(categoryId)) return null;
        return Categories.FirstOrDefault(c => c.Id == categoryId);
    }

    /// <summary>
    /// 添加工具。categoryName 是分类名，不存在则新建。
    /// </summary>
    /// <param name="categoryIcon">新建分类时使用的图标（Segoe MDL2 字形），已有分类忽略此参数</param>
    public bool AddTool(string categoryName, ToolConfig tool, string? categoryIcon = null)
    {
        lock (_lock)
        {
            var category = Categories.FirstOrDefault(c => c.Name == categoryName);
            if (category == null)
            {
                category = new CategoryConfig
                {
                    Id = NewId(),
                    Name = categoryName,
                    Icon = string.IsNullOrWhiteSpace(categoryIcon) ? DefaultCategoryIcon : categoryIcon
                };
                Categories.Add(category);
                Logger.Log($"创建新分类: {categoryName}");
            }

            if (string.IsNullOrEmpty(category.Id)) category.Id = NewId();

            // 同一分类下路径重复才算重复；不同分类指向同一个程序是允许的
            if (category.Tools.Any(t => t.RelativePath.Equals(tool.RelativePath, StringComparison.OrdinalIgnoreCase)))
            {
                Logger.Log($"工具已存在: {tool.Name}");
                return false;
            }

            if (string.IsNullOrEmpty(tool.Id)) tool.Id = NewId();

            // 顺手记下体积，之后这个文件被搬走时才能靠「名字 + 体积」找回
            try
            {
                var full = Path.Combine(BaseDir, tool.RelativePath);
                if (File.Exists(full)) tool.Size = new FileInfo(full).Length;
            }
            catch
            {
                // 读不到就算了，下次启动 SyncSizes 会补
            }

            category.Tools.Add(tool);
            Save();

            CacheIcon(tool.RelativePath);

            Logger.Log($"添加工具: {tool.Name} -> {categoryName}");
            return true;
        }
    }

    /// <summary>
    /// 修改工具的名称与描述。路径不动——搬文件风险高，改名不该牵连文件系统。
    /// </summary>
    public bool UpdateTool(string toolId, string name, string description)
    {
        lock (_lock)
        {
            var found = FindTool(toolId);
            if (found == null) return false;

            found.Value.Tool.Name = name;
            found.Value.Tool.Description = description;
            Save();

            Logger.Log($"更新工具: {name}");
            return true;
        }
    }

    /// <summary>
    /// 调整分类内顺序：把 draggedId 插到 targetId 之前或之后，再按新顺序重排 order 值。
    /// </summary>
    public bool ReorderTool(string categoryId, string draggedId, string targetId, bool insertAfter)
    {
        lock (_lock)
        {
            var category = FindCategoryById(categoryId);
            if (category == null) return false;

            // 与界面排序规则保持一致：先 order，再名称
            var ordered = category.Tools
                .OrderBy(t => t.Order)
                .ThenBy(t => t.Name, StringComparer.CurrentCulture)
                .ToList();

            var from = ordered.FindIndex(t => t.Id == draggedId);
            var to = ordered.FindIndex(t => t.Id == targetId);
            if (from < 0 || to < 0 || from == to) return false;

            var moved = ordered[from];
            ordered.RemoveAt(from);

            var targetIndex = ordered.FindIndex(t => t.Id == targetId);
            if (targetIndex < 0) return false;

            ordered.Insert(insertAfter ? targetIndex + 1 : targetIndex, moved);

            for (var i = 0; i < ordered.Count; i++)
                ordered[i].Order = (i + 1) * 10;

            Save();
            Logger.Log($"调整顺序: {moved.Name} -> {category.Name} 第 {ordered.IndexOf(moved) + 1} 位");
            return true;
        }
    }

    /// <summary>
    /// 重命名分类：同步把 工具\旧名 目录改名，并批量改写该分类下所有工具的路径。
    /// 目录搬不动就整体中止，配置保持原样。
    /// </summary>
    public bool RenameCategory(string categoryId, string newName)
    {
        lock (_lock)
        {
            newName = (newName ?? "").Trim();

            var category = FindCategoryById(categoryId);
            if (category == null || newName.Length == 0) return false;
            if (string.Equals(category.Name, newName, StringComparison.Ordinal)) return false;

            if (Categories.Any(c => c.Id != categoryId &&
                                    string.Equals(c.Name, newName, StringComparison.OrdinalIgnoreCase)))
            {
                Logger.Log($"重命名分类失败：已存在同名分类 {newName}");
                return false;
            }

            var oldName = category.Name;
            var oldDir = Path.Combine(BaseDir, "工具", oldName);
            var newDir = Path.Combine(BaseDir, "工具", newName);

            // 第一步：搬目录（搬不动就退出，配置一个字节都不改）
            if (Directory.Exists(oldDir))
            {
                if (Directory.Exists(newDir))
                {
                    Logger.Log($"重命名分类失败：目标目录已存在 {newDir}");
                    return false;
                }

                try
                {
                    Directory.Move(oldDir, newDir);
                }
                catch (Exception ex)
                {
                    Logger.Log($"重命名分类失败（目录可能被占用）: {ex.Message}");
                    return false;
                }
            }

            // 第二步：批量改写路径前缀，并同步迁移图标缓存
            var oldPrefix = "工具" + Path.DirectorySeparatorChar + oldName + Path.DirectorySeparatorChar;
            var newPrefix = "工具" + Path.DirectorySeparatorChar + newName + Path.DirectorySeparatorChar;

            foreach (var tool in category.Tools)
            {
                if (!tool.RelativePath.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase)) continue;

                var oldPath = tool.RelativePath;
                tool.RelativePath = newPrefix + oldPath[oldPrefix.Length..];
                MoveCachedIcon(oldPath, tool.RelativePath);
            }

            category.Name = newName;
            Save();

            Logger.Log($"重命名分类: {oldName} -> {newName}");
            return true;
        }
    }

    /// <summary>路径变化时把图标缓存文件改名，避免重新提取</summary>
    private static void MoveCachedIcon(string oldRelativePath, string newRelativePath)
    {
        try
        {
            var oldIcon = GetIconCachePath(oldRelativePath);
            if (!File.Exists(oldIcon)) return;

            var newIcon = GetIconCachePath(newRelativePath);
            var dir = Path.GetDirectoryName(newIcon);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.Move(oldIcon, newIcon, true);
        }
        catch
        {
            // 迁移失败无所谓，下次启动会重新提取
        }
    }

    /// <summary>
    /// 删除工具：文件移入回收站 + 配置条目移入 trash，两者共用同一个 id。
    /// 先搬文件、再改配置——搬失败就整体中止，绝不出现"配置没了文件还在"。
    /// </summary>
    public bool MoveToolToTrash(string toolId)
    {
        lock (_lock)
        {
            var found = FindTool(toolId);
            if (found == null)
            {
                Logger.Log($"删除工具失败：配置中找不到 id={toolId}");
                return false;
            }

            var category = found.Value.Category;
            var tool = found.Value.Tool;

            var entry = new TrashEntry
            {
                Id = NewId()[..8],
                ToolId = tool.Id,
                CategoryId = category.Id,
                Name = tool.Name,
                Description = tool.Description,
                RelativePath = tool.RelativePath,
                Category = category.Name,
                Icon = category.Icon,
                DeletedAt = DateTime.Now
            };

            // 第一步：搬文件。失败直接返回，配置原封不动
            var src = Path.Combine(BaseDir, tool.RelativePath);
            if (File.Exists(src) || Directory.Exists(src))
            {
                try
                {
                    MoveIntoTrash(src, entry.Id);
                }
                catch (Exception ex)
                {
                    Logger.Log($"移入回收站失败（文件可能正在运行）: {src} - {ex.Message}");
                    return false;
                }
            }
            else
            {
                Logger.Log($"文件不存在，仅清理配置条目: {src}");
            }

            // 第二步：改配置
            category.Tools.Remove(tool);
            Trash.Insert(0, entry);
            Save();
            DeleteCachedIcon(tool.RelativePath);

            Logger.Log($"工具已移入回收站: {tool.Name} (trash={entry.Id})");
            return true;
        }
    }

    /// <summary>
    /// 删除整个分类：整个目录移入回收站，分类下所有工具配置一并暂存
    /// </summary>
    public bool MoveCategoryToTrash(string categoryId)
    {
        lock (_lock)
        {
            var category = FindCategoryById(categoryId);
            if (category == null) return false;

            var entry = new TrashEntry
            {
                Id = NewId()[..8],
                CategoryId = category.Id,
                Name = category.Name,
                Description = category.Description,
                RelativePath = Path.Combine("工具", category.Name),
                Category = category.Name,
                Icon = category.Icon,
                IsCategory = true,
                DeletedAt = DateTime.Now,
                Tools = category.Tools.Select(t => new ToolConfig
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    RelativePath = t.RelativePath,
                    Order = t.Order
                }).ToList()
            };

            var src = Path.Combine(BaseDir, "工具", category.Name);
            if (Directory.Exists(src))
            {
                try
                {
                    MoveIntoTrash(src, entry.Id);
                }
                catch (Exception ex)
                {
                    Logger.Log($"分类移入回收站失败: {src} - {ex.Message}");
                    return false;
                }
            }

            foreach (var tool in category.Tools)
                DeleteCachedIcon(tool.RelativePath);

            Categories.Remove(category);
            Trash.Insert(0, entry);
            Save();

            Logger.Log($"分类已移入回收站: {category.Name}，含 {category.Tools.Count} 个工具 (trash={entry.Id})");
            return true;
        }
    }

    /// <summary>
    /// 从回收站恢复（文件移回原位 + 配置条目重新挂回分类）。
    /// 恢复时沿用原来的分类 id / 工具 id，保证身份不因一次删除而改变。
    /// </summary>
    public bool RestoreFromTrash(string id)
    {
        lock (_lock)
        {
            var entry = Trash.FirstOrDefault(e => e.Id == id);
            if (entry == null) return false;

            var trashPath = GetTrashItemPath(entry);
            try
            {
                if (entry.IsCategory)
                {
                    var destDir = Path.Combine(BaseDir, entry.RelativePath);
                    if (Directory.Exists(trashPath))
                    {
                        if (Directory.Exists(destDir)) destDir = GetUniquePath(destDir, true);
                        Directory.CreateDirectory(Path.GetDirectoryName(destDir)!);
                        Directory.Move(trashPath, destDir);
                        entry.RelativePath = Path.GetRelativePath(BaseDir, destDir);
                    }

                    var category = FindCategory(entry);
                    if (category == null)
                    {
                        category = new CategoryConfig
                        {
                            Id = string.IsNullOrEmpty(entry.CategoryId) ? NewId() : entry.CategoryId,
                            Name = entry.Name,
                            Icon = entry.Icon,
                            Description = entry.Description
                        };
                        Categories.Add(category);
                    }

                    foreach (var tool in entry.Tools ?? new List<ToolConfig>())
                    {
                        if (category.Tools.Any(x => x.RelativePath.Equals(tool.RelativePath, StringComparison.OrdinalIgnoreCase)))
                            continue;

                        if (string.IsNullOrEmpty(tool.Id)) tool.Id = NewId();
                        category.Tools.Add(tool);
                    }
                }
                else
                {
                    var destPath = Path.Combine(BaseDir, entry.RelativePath);
                    if (File.Exists(trashPath) || Directory.Exists(trashPath))
                    {
                        if (File.Exists(destPath) || Directory.Exists(destPath))
                            destPath = GetUniquePath(destPath, Directory.Exists(trashPath));

                        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                        if (Directory.Exists(trashPath)) Directory.Move(trashPath, destPath);
                        else File.Move(trashPath, destPath);

                        entry.RelativePath = Path.GetRelativePath(BaseDir, destPath);
                    }

                    var category = FindCategory(entry);
                    if (category == null)
                    {
                        category = new CategoryConfig
                        {
                            Id = string.IsNullOrEmpty(entry.CategoryId) ? NewId() : entry.CategoryId,
                            Name = entry.Category,
                            Icon = entry.Icon
                        };
                        Categories.Add(category);
                    }

                    if (!category.Tools.Any(t => t.RelativePath.Equals(entry.RelativePath, StringComparison.OrdinalIgnoreCase)))
                    {
                        category.Tools.Add(new ToolConfig
                        {
                            Id = string.IsNullOrEmpty(entry.ToolId) ? NewId() : entry.ToolId,
                            Name = entry.Name,
                            Description = entry.Description,
                            RelativePath = entry.RelativePath
                        });
                    }
                }

                Trash.Remove(entry);
                Save();
                TryDeleteDir(Path.Combine(TrashDir, entry.Id));
                CacheIcon(entry.RelativePath);

                Logger.Log($"已从回收站恢复: {entry.Name} -> {entry.RelativePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"恢复失败: {entry.Name} - {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>恢复时找分类：优先按 id，其次按名称（旧数据没有 categoryId）</summary>
    private CategoryConfig? FindCategory(TrashEntry entry)
    {
        if (!string.IsNullOrEmpty(entry.CategoryId))
        {
            var byId = Categories.FirstOrDefault(c => c.Id == entry.CategoryId);
            if (byId != null) return byId;
        }

        var byName = Categories.FirstOrDefault(c => c.Name == entry.Category)
                     ?? Categories.FirstOrDefault(c => c.Name == entry.Name);

        return byName ?? Categories.FirstOrDefault();
    }

    /// <summary>
    /// 清空回收站（真正删除文件，不可恢复）
    /// </summary>
    public bool EmptyTrash()
    {
        lock (_lock)
        {
            try
            {
                foreach (var entry in Trash)
                    TryDeleteDir(Path.Combine(TrashDir, entry.Id));

                Trash.Clear();
                Save();
                Logger.Log("回收站已清空");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"清空回收站失败: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 回收站条目的实体文件路径
    /// </summary>
    private static string GetTrashItemPath(TrashEntry entry)
    {
        var name = Path.GetFileName(entry.RelativePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return Path.Combine(TrashDir, entry.Id, name);
    }

    private static void MoveIntoTrash(string source, string id)
    {
        var dir = Path.Combine(TrashDir, id);
        Directory.CreateDirectory(dir);

        var name = Path.GetFileName(source.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var dest = Path.Combine(dir, name);

        if (Directory.Exists(source)) Directory.Move(source, dest);
        else File.Move(source, dest);
    }

    private static string GetUniquePath(string path, bool isDirectory)
    {
        var dir = Path.GetDirectoryName(path)!;
        var name = isDirectory ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);
        var ext = isDirectory ? "" : Path.GetExtension(path);

        for (var i = 2; i < 1000; i++)
        {
            var candidate = Path.Combine(dir, $"{name} (恢复{i - 1}){ext}");
            if (!File.Exists(candidate) && !Directory.Exists(candidate)) return candidate;
        }
        return path;
    }

    private static void TryDeleteDir(string dir)
    {
        try
        {
            if (!Directory.Exists(dir)) return;
            ClearReadOnly(dir);
            Directory.Delete(dir, true);
        }
        catch (Exception ex)
        {
            Logger.Log($"删除目录失败: {dir} - {ex.Message}");
        }
    }

    private static void ClearReadOnly(string dir)
    {
        foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
        {
            var attr = File.GetAttributes(file);
            if (attr.HasFlag(FileAttributes.ReadOnly))
                File.SetAttributes(file, attr & ~FileAttributes.ReadOnly);
        }
    }

    /// <summary>
    /// 获取所有工具列表（扁平化）
    /// </summary>
    public List<ToolInfo> GetAllTools()
    {
        var tools = new List<ToolInfo>();
        foreach (var category in Categories)
        {
            foreach (var tool in category.Tools)
            {
                tools.Add(new ToolInfo(
                    Name: tool.Name,
                    Category: category.Name,
                    Icon: category.Icon,
                    Description: tool.Description,
                    RelativePath: tool.RelativePath,
                    Order: tool.Order,
                    Id: tool.Id,
                    CategoryId: category.Id
                ));
            }
        }
        return tools;
    }

    /// <summary>
    /// 获取所有分类列表
    /// </summary>
    public List<CatInfo> GetCategories()
    {
        return Categories.Select(c => new CatInfo(
            Id: c.Id,
            Name: c.Name,
            IconGlyph: c.Icon,
            Subtitle: c.Description
        )).ToList();
    }

    /// <summary>
    /// 缓存图标
    /// </summary>
    public static void CacheIcon(string relativePath)
    {
        try
        {
            var baseDir = BaseDir;
            var fullPath = Path.Combine(baseDir, relativePath);
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath)) return;

            var iconPath = GetIconCachePath(relativePath);
            var iconDir = Path.GetDirectoryName(iconPath);
            if (!string.IsNullOrEmpty(iconDir) && !Directory.Exists(iconDir))
                Directory.CreateDirectory(iconDir);

            // 提取图标并保存为 PNG
            var icon = IconExtractor.ToPngBytes(fullPath);
            if (icon != null)
            {
                File.WriteAllBytes(iconPath, icon);
                Logger.Log($"缓存图标: {relativePath}");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"缓存图标失败: {relativePath} - {ex.Message}");
        }
    }

    /// <summary>
    /// 删除缓存的图标
    /// </summary>
    public static void DeleteCachedIcon(string relativePath)
    {
        try
        {
            var iconPath = GetIconCachePath(relativePath);
            if (File.Exists(iconPath))
            {
                File.Delete(iconPath);
                Logger.Log($"删除图标缓存: {relativePath}");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"删除图标缓存失败: {relativePath} - {ex.Message}");
        }
    }

    /// <summary>
    /// 获取图标缓存路径
    /// </summary>
    public static string GetIconCachePath(string relativePath)
    {
        // 将路径转换为安全的文件名
        var safeName = relativePath
            .Replace('\\', '_')
            .Replace('/', '_')
            .Replace(':', '_')
            .Replace('*', '_')
            .Replace('?', '_')
            .Replace('"', '_')
            .Replace('<', '_')
            .Replace('>', '_')
            .Replace('|', '_');

        return Path.Combine(IconCacheDir, safeName + ".png");
    }

    /// <summary>
    /// 批量刷新图标缓存，返回真正重新提取的数量。
    ///
    /// 缓存新鲜度按"源文件修改时间"判断：用户替换成新版 exe 后，
    /// 图标会自动更新，不必手动删 IconCache。
    /// </summary>
    public int CacheAllIcons()
    {
        var baseDir = BaseDir;

        // 确保缓存目录存在
        if (!Directory.Exists(IconCacheDir))
            Directory.CreateDirectory(IconCacheDir);

        var refreshedCount = 0;

        foreach (var tool in GetAllTools())
        {
            var fullPath = Path.Combine(baseDir, tool.RelativePath);
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath)) continue;

            var iconPath = GetIconCachePath(tool.RelativePath);
            if (IsIconCacheFresh(fullPath, iconPath)) continue;

            CacheIcon(tool.RelativePath);
            refreshedCount++;
        }

        Logger.Log($"刷新图标缓存: {refreshedCount} 个");
        CleanupIconCache();
        return refreshedCount;
    }

    /// <summary>
    /// 清掉不再被任何工具引用的图标缓存。
    /// 历史遗留的绝对路径缓存（如 E__陈叔叔工具箱_工具_...）和已移除工具的缓存都在这里回收，
    /// 否则 IconCache 只增不减。
    /// </summary>
    private int CleanupIconCache()
    {
        try
        {
            if (!Directory.Exists(IconCacheDir)) return 0;

            var valid = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tool in GetAllTools())
                valid.Add(Path.GetFileName(GetIconCachePath(tool.RelativePath)));

            var removed = 0;
            foreach (var file in Directory.EnumerateFiles(IconCacheDir, "*.png"))
            {
                if (valid.Contains(Path.GetFileName(file))) continue;

                try
                {
                    File.Delete(file);
                    removed++;
                }
                catch { /* 被占用就下次再清 */ }
            }

            if (removed > 0) Logger.Log($"清理无主图标缓存: {removed} 个");
            return removed;
        }
        catch (Exception ex)
        {
            Logger.Log($"清理图标缓存失败: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 缓存文件是否比源文件新（源文件被替换或目录内容变化时视为过期）
    /// </summary>
    private static bool IsIconCacheFresh(string sourcePath, string iconPath)
    {
        try
        {
            if (!File.Exists(iconPath)) return false;

            var cacheTime = File.GetLastWriteTimeUtc(iconPath);
            var sourceTime = File.Exists(sourcePath)
                ? File.GetLastWriteTimeUtc(sourcePath)
                : Directory.GetLastWriteTimeUtc(sourcePath);

            return cacheTime >= sourceTime;
        }
        catch
        {
            // 取不到时间戳就当作过期，宁可多提取一次
            return false;
        }
    }
}
