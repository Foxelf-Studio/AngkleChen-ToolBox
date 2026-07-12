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
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    private static readonly string IconCacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IconCache");
    private static readonly object _lock = new();

    /// <summary>
    /// 配置数据
    /// </summary>
    [JsonPropertyName("categories")]
    public List<CategoryConfig> Categories { get; set; } = new();

    /// <summary>
    /// 分类配置
    /// </summary>
    public class CategoryConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = " ";

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
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("relativePath")]
        public string RelativePath { get; set; } = "";
    }

    /// <summary>
    /// 加载配置（如果不存在则创建默认配置）
    /// </summary>
    public static AppConfig Load()
    {
        lock (_lock)
        {
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
                        Logger.Log($"加载配置: {config.Categories.Count} 个分类");
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"加载配置失败: {ex.Message}");
                }
            }

            // 创建默认配置
            var defaultConfig = CreateDefaultConfig();
            defaultConfig.Save();
            Logger.Log("创建默认配置");
            return defaultConfig;
        }
    }

    /// <summary>
    /// 修复配置中的绝对路径为相对路径
    /// </summary>
    private void FixAbsolutePaths()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
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
                File.WriteAllText(ConfigPath, json);
                Logger.Log($"保存配置: {Categories.Count} 个分类");
            }
            catch (Exception ex)
            {
                Logger.Log($"保存配置失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 添加工具
    /// </summary>
    public bool AddTool(string categoryName, ToolConfig tool)
    {
        var category = Categories.FirstOrDefault(c => c.Name == categoryName);
        if (category == null)
        {
            // 创建新分类
            category = new CategoryConfig { Name = categoryName };
            Categories.Add(category);
            Logger.Log($"创建新分类: {categoryName}");
        }

        // 检查工具是否已存在
        if (category.Tools.Any(t => t.RelativePath.Equals(tool.RelativePath, StringComparison.OrdinalIgnoreCase)))
        {
            Logger.Log($"工具已存在: {tool.Name}");
            return false;
        }

        category.Tools.Add(tool);
        Save();

        // 提取并缓存图标
        CacheIcon(tool.RelativePath);

        Logger.Log($"添加工具: {tool.Name} -> {categoryName}");
        return true;
    }

    /// <summary>
    /// 删除工具
    /// </summary>
    public bool RemoveTool(string relativePath)
    {
        foreach (var category in Categories)
        {
            var tool = category.Tools.FirstOrDefault(t =>
                t.RelativePath.Equals(relativePath, StringComparison.OrdinalIgnoreCase));

            if (tool != null)
            {
                category.Tools.Remove(tool);
                Save();

                // 删除缓存的图标
                DeleteCachedIcon(relativePath);

                Logger.Log($"删除工具: {tool.Name}");
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 删除分类
    /// </summary>
    public bool RemoveCategory(string categoryName)
    {
        var category = Categories.FirstOrDefault(c => c.Name == categoryName);
        if (category == null) return false;

        // 删除该分类下所有工具的图标缓存
        foreach (var tool in category.Tools)
        {
            DeleteCachedIcon(tool.RelativePath);
        }

        Categories.Remove(category);
        Save();

        Logger.Log($"删除分类: {categoryName}，包含 {category.Tools.Count} 个工具");
        return true;
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
                    RelativePath: tool.RelativePath
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
            Id: c.Name,
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
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var fullPath = Path.Combine(baseDir, relativePath);
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath)) return;

            var iconPath = GetIconCachePath(relativePath);
            var iconDir = Path.GetDirectoryName(iconPath);
            if (!string.IsNullOrEmpty(iconDir) && !Directory.Exists(iconDir))
                Directory.CreateDirectory(iconDir);

            // 提取图标并保存为 PNG
            var icon = IconHelper.ExtractIconToFile(fullPath);
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
    /// 批量缓存所有工具图标
    /// </summary>
    public void CacheAllIcons()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // 确保缓存目录存在
        if (!Directory.Exists(IconCacheDir))
            Directory.CreateDirectory(IconCacheDir);

        var tools = GetAllTools();
        var cachedCount = 0;

        foreach (var tool in tools)
        {
            var iconPath = GetIconCachePath(tool.RelativePath);
            if (File.Exists(iconPath)) continue; // 已缓存

            var fullPath = Path.Combine(baseDir, tool.RelativePath);
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath)) continue;

            CacheIcon(tool.RelativePath);
            cachedCount++;
        }

        Logger.Log($"批量缓存图标: {cachedCount} 个");
    }

    /// <summary>
    /// 创建默认配置
    /// </summary>
    private static AppConfig CreateDefaultConfig()
    {
        var config = new AppConfig();

        // 按照指定顺序添加分类
        var categoryOrder = new List<(string name, string icon, string desc)>
        {
            ("娱乐工具", "", "音乐与视频播放"),
            ("实用工具", "", "系统安装与诊断"),
            ("搞机工具", "", "激活·调试·跑分"),
            ("文件工具", "", "下载·压缩·分区"),
            ("清理工具", "", "卸载·清理·拦截"),
            ("依赖", "", "运行库与框架"),
            ("CPU工具", "", "处理器检测与测试"),
            ("内存工具", "", "内存检测与管理"),
            ("显卡工具", "", "GPU检测与驱动"),
            ("硬盘工具", "", "磁盘检测与恢复"),
            ("烤鸡工具", "", "压力测试与烤机"),
            ("外设工具", "", "鼠标键盘测试"),
            ("显示器工具", "", "屏幕检测与校准"),
            ("综合检测", "", "硬件综合信息"),
            ("系统工具", "", "系统维护与优化"),
            ("游戏平台", "", "游戏启动器"),
        };

        foreach (var (name, icon, desc) in categoryOrder)
        {
            config.Categories.Add(new CategoryConfig
            {
                Name = name,
                Icon = icon,
                Description = desc
            });
        }

        // 添加默认工具
        AddDefaultTools(config);

        return config;
    }

    /// <summary>
    /// 添加默认工具
    /// </summary>
    private static void AddDefaultTools(AppConfig config)
    {
        var tools = new List<(string category, string name, string desc, string path)>
        {
            // 娱乐工具
            ("娱乐工具", "酷我音乐", "酷我音乐PC版，支持在线听歌、歌词显示、音效调节、本地音乐管理，支持无损音质下载。", @"工具\娱乐工具\KwMusic\KwMusic.exe"),
            ("娱乐工具", "PiliPlus", "第三方哔哩哔哩客户端，支持弹幕播放、视频下载、多清晰度切换，基于Flutter开发。", @"工具\娱乐工具\PiliPlus-Win\piliplus.exe"),
            ("娱乐工具", "洛雪音乐", "洛雪音乐桌面版，开源免费音乐播放器，支持多平台音源、无损音质、歌词显示、歌单导入。", @"工具\娱乐工具\洛雪音乐\lx-music-desktop.exe"),

            // 实用工具
            ("实用工具", "Edge 浏览器", "微软Edge浏览器在线安装程序，基于Chromium内核，支持扩展插件、集锦、沉浸式阅读器等功能。", @"工具\实用工具\edge\MicrosoftEdgeSetup.exe"),
            ("实用工具", "Office Tool Plus", "微软Office部署工具，支持Office 2016-2024及Microsoft 365的在线/离线安装、激活、更新管理。", @"工具\实用工具\Office Tool x64\Office Tool Plus.exe"),
            ("实用工具", "Office Tool (32位)", "Office Tool Plus 32位版本，适用于32位Windows系统，功能与64位版本相同。", @"工具\实用工具\Office Tool x32\Office Tool Plus.exe"),
            ("实用工具", "PCR532 读卡器", "基于PN532芯片的NFC读卡器工具，支持IC卡读写、UID读取、门禁卡模拟、Mifare Classic破解。", @"工具\实用工具\pcr532\PN532CardReader.exe"),
            ("实用工具", "Intel 核显驱动", "Intel核芯显卡驱动离线安装包，支持HD/UHD/Iris系列核显，适用于Windows 10/11系统。", @"工具\实用工具\核显驱动\Intelhxqdwin10.exe"),
            ("实用工具", "WPS 2019", "WPS Office 2019专业版安装程序，兼容微软Office格式，支持文字、表格、演示三大组件。", @"工具\实用工具\wps2019\W.P.S.10314.12012.2019.exe"),
            ("实用工具", "网卡驱动", "360驱动大师网卡版，内置万能网卡驱动，解决重装系统后无法联网的问题，支持有线/无线网卡。", @"工具\实用工具\网卡驱动\gwwk__360DrvMgrInstaller_net.exe"),
            ("实用工具", "自动更新时间", "Seewo网络时间同步工具，通过NTP服务器校准系统时间，适用于教育一体机等设备。", @"工具\实用工具\自动更新系统时间\SeewoOpt.exe"),

            // 搞机工具
            ("搞机工具", "HEU KMS 激活", "HEU KMS Activator，支持Windows Vista~11及Office 2010~2024的KMS激活，无需联网，操作简单。", @"工具\搞机工具\HEU激活工具\HEU_KMS_Activator_v63.3.0.exe"),
            ("搞机工具", "沧水 KMS", "沧水KMS激活批处理脚本，通过KMS服务器激活Windows和Office，支持多种版本。", @"工具\搞机工具\沧水KMS\KMS-Cangshui.net.bat"),
            ("搞机工具", "安卓手表 ADB", "安卓手表ADB调试工具，支持安装应用、传输文件、修改系统设置，适用于各类安卓智能手表。", @"工具\搞机工具\安卓手表adb实用工具箱\安卓手表ADB实用工具箱 66.0.0.exe"),
            ("搞机工具", "鲁大师", "鲁大师硬件检测工具，支持CPU/显卡/内存/硬盘性能跑分，温度监控，驱动管理。", @"工具\搞机工具\鲁大师\ludashisetup.exe"),

            // 文件工具
            ("文件工具", "图压", "图压图片压缩工具，支持JPG/PNG/GIF/WebP批量压缩，保持画质的同时大幅减小文件体积。", @"工具\文件工具\图压\图压.exe"),
            ("文件工具", "磁盘精灵", "DiskGenius专业版，支持磁盘分区管理、数据恢复、备份还原、分区迁移、坏道检测修复。", @"工具\文件工具\磁盘精灵\DiskGenius-Pro-v6.0.0.1631-x64-Chs.exe"),
            ("文件工具", "迅雷", "迅雷极速版，支持BT/磁力链接/HTTP多线程下载，P2P加速，离线下载，边下边播。", @"工具\文件工具\迅雷\program\thunder.exe"),
            ("文件工具", "格式转换", "格式工厂等在线格式转换工具集合，支持视频、音频、图片、文档等格式互相转换。", @"工具\文件工具\格式转换工具"),

            // 清理工具
            ("清理工具", "HiBit Uninstaller", "HiBit Uninstaller，支持强制卸载、批量卸载、注册表清理、垃圾文件清理、启动项管理。", @"工具\清理工具\hibit\HiBitUninstaller.exe"),
            ("清理工具", "IObit Uninstaller", "IObit Uninstaller专业版便携版，支持强制卸载、软件健康检测、安装监视、浏览器插件清理。", @"工具\清理工具\IOBit\IObitUninstaller-Pro-14.4.0.3-Portable.exe"),
            ("清理工具", "Geek Uninstaller", "Geek Uninstaller轻量级卸载工具，支持强制卸载、清理残留、管理Windows Store应用。", @"工具\清理工具\Geek Uninstaller\Geek Uninstaller.exe"),
            ("清理工具", "SoftCnKiller", "SoftCnKiller，专治国内流氓软件，支持查杀恶意程序、拦截弹窗广告、修复浏览器劫持。", @"工具\清理工具\SoftCnKiller2.85\SoftCnKiller.exe"),

            // 依赖
            ("依赖", ".NET Framework 4.7", ".NET Framework 4.7.2离线安装包，许多软件和游戏的运行依赖，支持Windows 7/8/10。", @"工具\依赖\.net\NDP472-KB4054530-x86-x64-AllOS-ENU.exe"),
            ("依赖", "VC++ 运行库", "Microsoft Visual C++ 2005-2022运行库合集，许多软件和游戏的必要依赖组件。", @"工具\依赖\vc\vc_redist.x86.exe"),

            // CPU工具
            ("CPU工具", "CPU-Z", "CPU-Z处理器检测工具，显示CPU型号、核心数、频率、缓存、主板、内存等详细硬件信息。", @"工具\CPU工具\CPUZ\cpuz_x64.exe"),
            ("CPU工具", "Core Temp", "Core Temp轻量级CPU温度监控工具，实时显示各核心温度、频率、负载，支持高温报警。", @"工具\CPU工具\CoreTemp\Core Temp x64.exe"),
            ("CPU工具", "ThrottleStop", "ThrottleStop CPU降压工具，支持调整CPU电压、频率、功耗限制，降低温度延长续航。", @"工具\CPU工具\ThrottleStop\ThrottleStop.exe"),
            ("CPU工具", "Prime95", "Prime95 CPU压力测试工具，通过梅森素数计算对CPU进行极限拷机，检测稳定性。", @"工具\CPU工具\Prime95\prime95.exe"),
            ("CPU工具", "SuperPI", "SuperPI圆周率计算测试，通过计算PI值测试CPU单核性能和稳定性，经典跑分工具。", @"工具\CPU工具\superpi\Superpi.exe"),
            ("CPU工具", "wPrime", "wPrime多线程CPU基准测试工具，通过计算平方根值测试CPU多核性能。", @"工具\CPU工具\wPrime\wPrime.exe"),
            ("CPU工具", "LinX", "LinX基于Intel Linpack的CPU压力测试工具，比Prime95更严格，能快速发现CPU不稳定问题。", @"工具\CPU工具\LinX\LinX.exe"),

            // 内存工具
            ("内存工具", "MemTest", "MemTest内存错误检测工具，通过反复读写测试内存条是否存在坏块，建议运行至少1轮。", @"工具\内存工具\memtest\memtest.exe"),
            ("内存工具", "MemTest64", "MemTest64 64位内存测试工具，可在Windows下直接测试内存，无需进入DOS环境。", @"工具\内存工具\memtest64\MemTest64.exe"),
            ("内存工具", "Thaiphoon", "Thaiphoon Burner内存颗粒信息读取工具，显示内存厂商、颗粒型号、时序参数、XMP信息。", @"工具\内存工具\Thaiphoon\Thaiphoon.exe"),
            ("内存工具", "TM5", "TM5内存稳定性测试工具，配合自定义配置文件可精确检测内存超频后的稳定性。", @"工具\内存工具\tm5\TM5.exe"),
            ("内存工具", "ZenTimings", "ZenTimings AMD平台内存时序查看工具，显示频率、时序、电压、Gear Down Mode等参数。", @"工具\内存工具\ZenTimings\ZenTimings.exe"),
            ("内存工具", "魔方内存盘", "魔方内存盘，将内存虚拟为硬盘分区，读写速度极快，适合存放临时文件和缓存。", @"工具\内存工具\魔方内存盘\ramdisk.exe"),

            // 显卡工具
            ("显卡工具", "GPU-Z", "GPU-Z显卡信息检测工具，显示GPU型号、显存、频率、温度、功耗等详细参数。", @"工具\显卡工具\GPUZ\GPU-Z.exe"),
            ("显卡工具", "DDU", "DDU(Display Driver Uninstaller)显卡驱动彻底卸载工具，支持NVIDIA/AMD/Intel驱动清理。", @"工具\显卡工具\DDU\Display Driver Uninstaller.exe"),
            ("显卡工具", "nvidiaInspector", "nvidiaInspector NVIDIA显卡参数调节工具，支持调整频率、电压、功耗限制、风扇曲线。", @"工具\显卡工具\nvidiaInspector\nvidiaInspector.exe"),
            ("显卡工具", "DXVAChecker", "DXVAChecker视频硬件加速能力检测工具，显示GPU支持的解码格式和编码能力。", @"工具\显卡工具\dxvachecker\DXVAChecker.exe"),
            ("显卡工具", "AMD驱动下载", "AMD显卡驱动官方下载页面，提供Radeon系列显卡最新驱动程序下载。", @"工具\显卡工具\AMD显卡驱动下载\Start.bat"),
            ("显卡工具", "Nvidia驱动下载", "NVIDIA显卡驱动官方下载页面，提供GeForce系列显卡最新Game Ready驱动下载。", @"工具\显卡工具\Nvidia显卡驱动下载\Start.bat"),
            ("显卡工具", "MSI Afterburner", "MSI Afterburner显卡超频工具下载页面，支持NVIDIA/AMD显卡频率调节、监控、录像。", @"工具\显卡工具\MSIAfterburnerSetup\start.bat"),

            // 硬盘工具
            ("硬盘工具", "CrystalDiskInfo", "CrystalDiskInfo硬盘健康检测工具，读取S.M.A.R.T.信息，显示硬盘温度、通电时间、健康状态。", @"工具\硬盘工具\CrystalDiskInfo\DiskInfo64S.exe"),
            ("硬盘工具", "CrystalDiskMark", "CrystalDiskMark硬盘读写速度测试工具，支持顺序/随机读写测试，支持NVMe/SATA/USB设备。", @"工具\硬盘工具\CrystalDiskMark\DiskMark64S.exe"),
            ("硬盘工具", "AS SSD Benchmark", "AS SSD Benchmark固态硬盘性能测试工具，测试顺序/4K读写、访问时间、综合评分。", @"工具\硬盘工具\ASSSDBenchmark\ASSSDBenchmark.exe"),
            ("硬盘工具", "HDTune", "HDTune硬盘综合工具，支持基准测试、错误扫描、健康检测、安全擦除等功能。", @"工具\硬盘工具\HDTune\HDTune.exe"),
            ("硬盘工具", "SpaceSniffer", "SpaceSniffer磁盘空间可视化分析工具，以树状图方式直观显示文件夹占用空间大小。", @"工具\硬盘工具\SpaceSniffer\SpaceSniffer.exe"),
            ("硬盘工具", "WizTree", "WizTree超高速磁盘空间分析工具，直接读取MFT表，比同类工具快100倍以上。", @"工具\硬盘工具\WizTree\WizTree.exe"),
            ("硬盘工具", "Defraggler", "Defraggler磁盘碎片整理工具，支持整理整个磁盘或指定文件/文件夹，支持SSD优化。", @"工具\硬盘工具\Defraggler\Defraggler.exe"),
            ("硬盘工具", "FinalData", "FinalData数据恢复工具，支持恢复误删除文件、格式化恢复、分区丢失恢复。", @"工具\硬盘工具\finaldata\FINALDATA.exe"),
            ("硬盘工具", "魔方数据恢复", "魔方数据恢复工具，支持恢复误删除、误格式化、分区丢失的数据，操作简单。", @"工具\硬盘工具\魔方数据恢复\魔方数据恢复.exe"),
            ("硬盘工具", "TxBENCH", "TxBENCH固态硬盘基准测试工具，支持多种测试模式，可测试NVMe/SATA SSD性能。", @"工具\硬盘工具\TxBENCH\TxBENCH.exe"),
            ("硬盘工具", "SSDZ", "SSDZ固态硬盘检测工具，显示SSD型号、固件版本、通电时间、写入量等信息。", @"工具\硬盘工具\SSDZ\SSDZ.exe"),

            // 烤鸡工具
            ("烤鸡工具", "FurMark", "FurMark GPU压力测试工具，通过OpenGL高负载渲染测试显卡稳定性和散热性能，俗称烤机。", @"工具\烤鸡工具\furmark.exe"),
            ("烤鸡工具", "FurMark GUI", "FurMark GUI版GPU烤机工具，支持自定义分辨率、抗锯齿、运行时间等参数。", @"工具\烤鸡工具\FurMark_GUI.exe"),

            // 外设工具
            ("外设工具", "鼠标测试", "AresonMouseTest鼠标性能测试工具，测试鼠标回报率、按键响应、DPI精度、丢帧情况。", @"工具\外设工具\AresonMouseTest\鼠标测试软件AresonMouseTestProgram.exe"),
            ("外设工具", "键盘测试", "Keyboard Test Utility键盘按键检测工具，实时显示按键状态，检测按键冲突和失灵。", @"工具\外设工具\Keyboard Test Utility\Keyboard Test Utility.exe"),
            ("外设工具", "KeyTweak", "KeyTweak键盘按键映射工具，支持禁用、重映射键盘按键，修改键盘布局。", @"工具\外设工具\KeyTweak\KeyTweak.exe"),
            ("外设工具", "MouseTester", "MouseTester鼠标回报率测试工具，精确测量鼠标USB回报率和移动轨迹平滑度。", @"工具\外设工具\MouseTester\MouseTester.exe"),
            ("外设工具", "在线外设测试", "在线外设测试中心，通过浏览器测试键盘按键、鼠标点击、鼠标回报率等外设性能。", @"工具\外设工具\在线外设测试中心\在线外设测试中心.bat"),

            // 显示器工具
            ("显示器工具", "色域检测", "MonitorInfo显示器色域检测工具，读取显示器EDID信息，显示色域、分辨率、刷新率等参数。", @"工具\显示器工具\色域检测\monitorinfo.exe"),
            ("显示器工具", "UFO显示器测试", "UFO Test在线显示器刷新率测试工具，通过浏览器测试显示器实际刷新率和帧率表现。", @"工具\显示器工具\UFO测试\Start.bat"),
            ("显示器工具", "在线屏幕测试", "在线屏幕检测工具，通过浏览器检测显示器坏点、亮点、色彩均匀性、响应速度。", @"工具\显示器工具\在线屏幕测试\在线屏幕测试.bat"),

            // 综合检测
            ("综合检测", "AIDA64", "AIDA64硬件综合检测工具，支持CPU/内存/显卡/硬盘全面检测和压力测试，硬件信息最全。", @"工具\综合检测\AIDA64\aida64.exe"),
            ("综合检测", "HWiNFO", "HWiNFO硬件信息监控工具，实时显示CPU/GPU温度、频率、电压、功耗等传感器数据。", @"工具\综合检测\hwinfo\HWiNFO64.exe"),
            ("综合检测", "HWMonitor", "HWMonitor硬件温度监控工具，显示CPU/GPU/硬盘温度、风扇转速、电压等传感器数据。", @"工具\综合检测\HWMonitor\HWMonitor_x64.exe"),
            ("综合检测", "Speccy", "Speccy系统信息查看工具，显示操作系统、CPU、内存、主板、显卡、硬盘等详细硬件信息。", @"工具\综合检测\speccy\Speccy64.exe"),

            // 系统工具
            ("系统工具", "Dism++", "Dism++系统优化清理工具，支持系统精简、更新清理、驱动管理、启动项管理、系统备份。", @"工具\系统工具\Dism++\Dism++x64.exe"),
            ("系统工具", "DirectX Repair", "DirectX Repair DirectX组件修复工具，修复缺失的DLL文件，解决游戏无法运行的问题。", @"工具\系统工具\DirectX_Repair\DirectX Repair.exe"),
            ("系统工具", "Process Explorer", "Process Explorer高级进程管理工具，显示进程树、DLL依赖、句柄信息，比任务管理器更强大。", @"工具\系统工具\procexp\procexp64.exe"),
            ("系统工具", "WinDbg", "WinDbg Windows调试工具，用于分析蓝屏dump文件、调试驱动程序和应用程序崩溃。", @"工具\系统工具\WinDbg\windbg.exe"),
            ("系统工具", "Everything", "Everything极速文件搜索工具，基于NTFS索引，毫秒级搜索全盘文件，支持正则表达式。", @"工具\系统工具\Everything\everything.exe"),
            ("系统工具", "FanControl", "FanControl开源风扇控制工具，支持根据CPU/GPU温度自定义风扇转速曲线。", @"工具\系统工具\FanControl\FanControl.exe"),
            ("系统工具", "Rufus", "Rufus USB启动盘制作工具，支持Windows/Linux启动盘，支持UEFI/Legacy，速度极快。", @"工具\系统工具\rufus\rufus.exe"),
            ("系统工具", "Ventoy", "Ventoy多系统启动盘制作工具，只需拷贝ISO文件即可启动，无需反复格式化U盘。", @"工具\系统工具\ventoy\Ventoy2Disk.exe"),
            ("系统工具", "UltraISO", "UltraISO ISO镜像编辑工具，支持制作、编辑、转换ISO文件，支持直接提取引导信息。", @"工具\系统工具\ULTRAISO\ULTRAISO.exe"),
            ("系统工具", "BlueScreenView", "BlueScreenView蓝屏日志分析工具，自动读取蓝屏dump文件，显示导致蓝屏的驱动文件。", @"工具\系统工具\bluescreenview\BlueScreenViewx64.exe"),
            ("系统工具", "BatteryInfo", "BatteryInfoView笔记本电池信息查看工具，显示电池容量、健康度、充放电次数、制造商信息。", @"工具\系统工具\BatteryInfoView\BatteryInfoView.exe"),
            ("系统工具", "next.itellyou", "next.itellyou Windows原版镜像下载站，提供Windows 7~11各版本官方ISO镜像下载。", @"工具\系统工具\next_itellyou\Start.bat"),

            // 游戏平台
            ("游戏平台", "Steam 下载", "Steam游戏平台下载页面，全球最大的PC游戏平台，支持游戏购买、下载、社区功能。", @"工具\游戏平台\Steam\下载Steam.bat"),
            ("游戏平台", "Epic Games下载", "Epic Games游戏平台，每周免费游戏领取，支持虚幻引擎游戏库和社交功能。", @"工具\游戏平台\epic\Start.bat"),
            ("游戏平台", "EA App下载", "EA App(EA桌面应用)，EA旗下游戏平台，管理EA游戏库、订阅EA Play服务。", @"工具\游戏平台\eaapp\Start.bat"),
            ("游戏平台", "战网下载", "暴雪战网客户端，下载和管理暴雪旗下游戏，包括魔兽世界、守望先锋、暗黑破坏神等。", @"工具\游戏平台\battle\Start.bat"),
        };

        foreach (var (category, name, desc, path) in tools)
        {
            var cat = config.Categories.FirstOrDefault(c => c.Name == category);
            if (cat != null)
            {
                cat.Tools.Add(new ToolConfig
                {
                    Name = name,
                    Description = desc,
                    RelativePath = path
                });
            }
        }
    }
}
