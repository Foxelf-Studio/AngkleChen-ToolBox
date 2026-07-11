using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using 陈叔叔工具箱.Controls;
using 陈叔叔工具箱.Helpers;
using 陈叔叔工具箱.Models;

using DoubleAnimation = System.Windows.Media.Animation.DoubleAnimation;
using PowerEase = System.Windows.Media.Animation.PowerEase;
using EasingMode = System.Windows.Media.Animation.EasingMode;
using Storyboard = System.Windows.Media.Animation.Storyboard;
using TranslateTransform = System.Windows.Media.TranslateTransform;

namespace 陈叔叔工具箱;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    // ── 工具箱根目录 ──────────────────────────────
    private static readonly string ToolboxRoot = AppDomain.CurrentDomain.BaseDirectory;

    // ── 分类 ──────────────────────────────────────
    private record CatInfo(string Id, string Name, string IconGlyph, string Subtitle);

    // Segoe MDL2 Assets — Windows 内置官方图标字体
    private CatInfo[] Categories =
    [
        new("all",   "全部工具",   "", "浏览所有便携工具"),
        new("娱乐工具",   "娱乐工具",   "", "音乐与视频播放"),
        new("实用工具",   "实用工具",   "", "系统安装与诊断"),
        new("搞机工具",   "搞机工具",   "", "激活·调试·跑分"),
        new("文件工具",   "文件工具",   "", "下载·压缩·分区"),
        new("清理工具",   "清理工具",   "", "卸载·清理·拦截"),
        new("依赖",   "依赖",   "", "运行库与框架"),
        new("CPU工具",  "CPU工具",   "", "处理器检测与测试"),
        new("内存工具",  "内存工具",   "", "内存检测与管理"),

        new("显卡工具",  "显卡工具",   "", "GPU检测与驱动"),
        new("硬盘工具",  "硬盘工具",   "", "磁盘检测与恢复"),
        new("烤鸡工具",  "烤鸡工具",   "", "压力测试与烤机"),
        new("外设工具",  "外设工具",   "", "鼠标键盘测试"),
        new("显示器工具", "显示器工具",  "", "屏幕检测与校准"),
        new("综合检测",  "综合检测",   "", "硬件综合信息"),
        new("系统工具",  "系统工具",   "", "系统维护与优化"),
        new("游戏平台",  "游戏平台",   "", "游戏启动器"),
    ];

    // ── 工具数据 ──────────────────────────────────
    private ToolInfo[] AllTools =
    [
        new("酷我音乐",      "娱乐工具", " ", "酷我音乐PC版，支持在线听歌、歌词显示、音效调节、本地音乐管理，支持无损音质下载。",     @"工具\娱乐工具\KwMusic\KwMusic.exe"),
        new("PiliPlus",     "娱乐工具", " ", "第三方哔哩哔哩客户端，支持弹幕播放、视频下载、多清晰度切换，基于Flutter开发。",         @"工具\娱乐工具\PiliPlus-Win\piliplus.exe"),
        new("洛雪音乐",     "娱乐工具", " ", "洛雪音乐桌面版，开源免费音乐播放器，支持多平台音源、无损音质、歌词显示、歌单导入。", @"工具\娱乐工具\洛雪音乐\lx-music-desktop.exe"),
        new("Edge 浏览器",   "实用工具", " ", "微软Edge浏览器在线安装程序，基于Chromium内核，支持扩展插件、集锦、沉浸式阅读器等功能。",     @"工具\实用工具\edge\MicrosoftEdgeSetup.exe"),
        new("Office Tool Plus","实用工具"," ","微软Office部署工具，支持Office 2016-2024及Microsoft 365的在线/离线安装、激活、更新管理。",    @"工具\实用工具\Office Tool x64\Office Tool Plus.exe"),
        new("Office Tool (32位)","实用工具"," ","Office Tool Plus 32位版本，适用于32位Windows系统，功能与64位版本相同。", @"工具\实用工具\Office Tool x32\Office Tool Plus.exe"),
        new("PCR532 读卡器", "实用工具", " ", "基于PN532芯片的NFC读卡器工具，支持IC卡读写、UID读取、门禁卡模拟、Mifare Classic破解。",       @"工具\实用工具\pcr532\PN532CardReader.exe"),
        new("Intel 核显驱动", "实用工具", " ", "Intel核芯显卡驱动离线安装包，支持HD/UHD/Iris系列核显，适用于Windows 10/11系统。",    @"工具\实用工具\核显驱动\Intelhxqdwin10.exe"),
        new("WPS 2019",      "实用工具", " ", "WPS Office 2019专业版安装程序，兼容微软Office格式，支持文字、表格、演示三大组件。",         @"工具\实用工具\wps2019\W.P.S.10314.12012.2019.exe"),
        new("网卡驱动",      "实用工具", " ", "360驱动大师网卡版，内置万能网卡驱动，解决重装系统后无法联网的问题，支持有线/无线网卡。",      @"工具\实用工具\网卡驱动\gwwk__360DrvMgrInstaller_net.exe"),
        new("自动更新时间",  "实用工具", " ", "Seewo网络时间同步工具，通过NTP服务器校准系统时间，适用于教育一体机等设备。",         @"工具\实用工具\自动更新系统时间\SeewoOpt.exe"),
        new("HEU KMS 激活",  "搞机工具", " ", "HEU KMS Activator，支持Windows Vista~11及Office 2010~2024的KMS激活，无需联网，操作简单。",  @"工具\搞机工具\HEU激活工具\HEU_KMS_Activator_v63.3.0.exe"),
        new("沧水 KMS",     "搞机工具", " ", "沧水KMS激活批处理脚本，通过KMS服务器激活Windows和Office，支持多种版本。",           @"工具\搞机工具\沧水KMS\KMS-Cangshui.net.bat"),
        new("安卓手表 ADB",  "搞机工具", " ", "安卓手表ADB调试工具，支持安装应用、传输文件、修改系统设置，适用于各类安卓智能手表。",    @"工具\搞机工具\安卓手表adb实用工具箱\安卓手表ADB实用工具箱 66.0.0.exe"),
        new("鲁大师",       "搞机工具", " ", "鲁大师硬件检测工具，支持CPU/显卡/内存/硬盘性能跑分，温度监控，驱动管理。",         @"工具\搞机工具\鲁大师\ludashisetup.exe"),

        new("图压",         "文件工具", " ", "图压图片压缩工具，支持JPG/PNG/GIF/WebP批量压缩，保持画质的同时大幅减小文件体积。",       @"工具\文件工具\图压\图压.exe"),
        new("磁盘精灵",     "文件工具", " ", "DiskGenius专业版，支持磁盘分区管理、数据恢复、备份还原、分区迁移、坏道检测修复。",    @"工具\文件工具\磁盘精灵\DiskGenius-Pro-v6.0.0.1631-x64-Chs.exe"),
        new("迅雷",         "文件工具", " ", "迅雷极速版，支持BT/磁力链接/HTTP多线程下载，P2P加速，离线下载，边下边播。",           @"工具\文件工具\迅雷\program\thunder.exe"),
        new("格式转换",     "文件工具", " ", "格式工厂等在线格式转换工具集合，支持视频、音频、图片、文档等格式互相转换。",        @"工具\文件工具\格式转换工具"),
        new("HiBit Uninstaller","清理工具"," ","HiBit Uninstaller，支持强制卸载、批量卸载、注册表清理、垃圾文件清理、启动项管理。",   @"工具\清理工具\hibit\HiBitUninstaller.exe"),
        new("IObit Uninstaller","清理工具"," ","IObit Uninstaller专业版便携版，支持强制卸载、软件健康检测、安装监视、浏览器插件清理。",       @"工具\清理工具\IOBit\IObitUninstaller-Pro-14.4.0.3-Portable.exe"),
        new("Geek Uninstaller","清理工具"," ","Geek Uninstaller轻量级卸载工具，支持强制卸载、清理残留、管理Windows Store应用。",      @"工具\清理工具\Geek Uninstaller\Geek Uninstaller.exe"),
        new("SoftCnKiller", "清理工具", " ", "SoftCnKiller，专治国内流氓软件，支持查杀恶意程序、拦截弹窗广告、修复浏览器劫持。", @"工具\清理工具\SoftCnKiller2.85\SoftCnKiller.exe"),
        new(".NET Framework 4.7", "依赖", " ", ".NET Framework 4.7.2离线安装包，许多软件和游戏的运行依赖，支持Windows 7/8/10。",        @"工具\依赖\.net\NDP472-KB4054530-x86-x64-AllOS-ENU.exe"),
        new("VC++ 运行库",   "依赖", " ", "Microsoft Visual C++ 2005-2022运行库合集，许多软件和游戏的必要依赖组件。",    @"工具\依赖\vc\vc_redist.x86.exe"),

        // CPU工具
        new("CPU-Z",          "CPU工具", " ", "CPU-Z处理器检测工具，显示CPU型号、核心数、频率、缓存、主板、内存等详细硬件信息。",       @"工具\CPU工具\CPUZ\cpuz_x64.exe"),
        new("Core Temp",      "CPU工具", " ", "Core Temp轻量级CPU温度监控工具，实时显示各核心温度、频率、负载，支持高温报警。",        @"工具\CPU工具\CoreTemp\Core Temp x64.exe"),
        new("ThrottleStop",   "CPU工具", " ", "ThrottleStop CPU降压工具，支持调整CPU电压、频率、功耗限制，降低温度延长续航。",        @"工具\CPU工具\ThrottleStop\ThrottleStop.exe"),
        new("Prime95",        "CPU工具", " ", "Prime95 CPU压力测试工具，通过梅森素数计算对CPU进行极限拷机，检测稳定性。",       @"工具\CPU工具\Prime95\prime95.exe"),
        new("SuperPI",        "CPU工具", " ", "SuperPI圆周率计算测试，通过计算PI值测试CPU单核性能和稳定性，经典跑分工具。",      @"工具\CPU工具\superpi\Superpi.exe"),
        new("wPrime",         "CPU工具", " ", "wPrime多线程CPU基准测试工具，通过计算平方根值测试CPU多核性能。",       @"工具\CPU工具\wPrime\wPrime.exe"),
        new("LinX",           "CPU工具", " ", "LinX基于Intel Linpack的CPU压力测试工具，比Prime95更严格，能快速发现CPU不稳定问题。",     @"工具\CPU工具\LinX\LinX.exe"),

        // 内存工具
        new("MemTest",        "内存工具", " ", "MemTest内存错误检测工具，通过反复读写测试内存条是否存在坏块，建议运行至少1轮。",       @"工具\内存工具\memtest\memtest.exe"),
        new("MemTest64",      "内存工具", " ", "MemTest64 64位内存测试工具，可在Windows下直接测试内存，无需进入DOS环境。",       @"工具\内存工具\memtest64\MemTest64.exe"),
        new("Thaiphoon",      "内存工具", " ", "Thaiphoon Burner内存颗粒信息读取工具，显示内存厂商、颗粒型号、时序参数、XMP信息。",       @"工具\内存工具\Thaiphoon\Thaiphoon.exe"),
        new("TM5",            "内存工具", " ", "TM5内存稳定性测试工具，配合自定义配置文件可精确检测内存超频后的稳定性。",      @"工具\内存工具\tm5\TM5.exe"),
        new("ZenTimings",     "内存工具", " ", "ZenTimings AMD平台内存时序查看工具，显示频率、时序、电压、Gear Down Mode等参数。",     @"工具\内存工具\ZenTimings\ZenTimings.exe"),
        new("魔方内存盘",     "内存工具", " ", "魔方内存盘，将内存虚拟为硬盘分区，读写速度极快，适合存放临时文件和缓存。",       @"工具\内存工具\魔方内存盘\ramdisk.exe"),

        // 显卡工具
        new("GPU-Z",          "显卡工具", " ", "GPU-Z显卡信息检测工具，显示GPU型号、显存、频率、温度、功耗等详细参数。",        @"工具\显卡工具\GPUZ\GPU-Z.exe"),
        new("DDU",            "显卡工具", " ", "DDU(Display Driver Uninstaller)显卡驱动彻底卸载工具，支持NVIDIA/AMD/Intel驱动清理。",       @"工具\显卡工具\DDU\Display Driver Uninstaller.exe"),
        new("nvidiaInspector","显卡工具", " ", "nvidiaInspector NVIDIA显卡参数调节工具，支持调整频率、电压、功耗限制、风扇曲线。",     @"工具\显卡工具\nvidiaInspector\nvidiaInspector.exe"),
        new("DXVAChecker",    "显卡工具", " ", "DXVAChecker视频硬件加速能力检测工具，显示GPU支持的解码格式和编码能力。",    @"工具\显卡工具\dxvachecker\DXVAChecker.exe"),
        new("AMD驱动下载",   "显卡工具", " ", "AMD显卡驱动官方下载页面，提供Radeon系列显卡最新驱动程序下载。", @"工具\显卡工具\AMD显卡驱动下载\Start.bat"),
        new("Nvidia驱动下载","显卡工具", " ", "NVIDIA显卡驱动官方下载页面，提供GeForce系列显卡最新Game Ready驱动下载。", @"工具\显卡工具\Nvidia显卡驱动下载\Start.bat"),
        new("MSI Afterburner","显卡工具", " ", "MSI Afterburner显卡超频工具下载页面，支持NVIDIA/AMD显卡频率调节、监控、录像。", @"工具\显卡工具\MSIAfterburnerSetup\start.bat"),

        // 硬盘工具
        new("CrystalDiskInfo","硬盘工具", " ", "CrystalDiskInfo硬盘健康检测工具，读取S.M.A.R.T.信息，显示硬盘温度、通电时间、健康状态。",       @"工具\硬盘工具\CrystalDiskInfo\DiskInfo64S.exe"),
        new("CrystalDiskMark","硬盘工具", " ", "CrystalDiskMark硬盘读写速度测试工具，支持顺序/随机读写测试，支持NVMe/SATA/USB设备。",    @"工具\硬盘工具\CrystalDiskMark\DiskMark64S.exe"),
        new("AS SSD Benchmark","硬盘工具", " ", "AS SSD Benchmark固态硬盘性能测试工具，测试顺序/4K读写、访问时间、综合评分。",       @"工具\硬盘工具\ASSSDBenchmark\ASSSDBenchmark.exe"),
        new("HDTune",         "硬盘工具", " ", "HDTune硬盘综合工具，支持基准测试、错误扫描、健康检测、安全擦除等功能。",       @"工具\硬盘工具\HDTune\HDTune.exe"),
        new("SpaceSniffer",   "硬盘工具", " ", "SpaceSniffer磁盘空间可视化分析工具，以树状图方式直观显示文件夹占用空间大小。",     @"工具\硬盘工具\SpaceSniffer\SpaceSniffer.exe"),
        new("WizTree",        "硬盘工具", " ", "WizTree超高速磁盘空间分析工具，直接读取MFT表，比同类工具快100倍以上。",       @"工具\硬盘工具\WizTree\WizTree.exe"),
        new("Defraggler",     "硬盘工具", " ", "Defraggler磁盘碎片整理工具，支持整理整个磁盘或指定文件/文件夹，支持SSD优化。",       @"工具\硬盘工具\Defraggler\Defraggler.exe"),
        new("FinalData",      "硬盘工具", " ", "FinalData数据恢复工具，支持恢复误删除文件、格式化恢复、分区丢失恢复。",       @"工具\硬盘工具\finaldata\FINALDATA.exe"),
        new("魔方数据恢复",   "硬盘工具", " ", "魔方数据恢复工具，支持恢复误删除、误格式化、分区丢失的数据，操作简单。",       @"工具\硬盘工具\魔方数据恢复\魔方数据恢复.exe"),
        new("TxBENCH",        "硬盘工具", " ", "TxBENCH固态硬盘基准测试工具，支持多种测试模式，可测试NVMe/SATA SSD性能。",        @"工具\硬盘工具\TxBENCH\TxBENCH.exe"),
        new("SSDZ",           "硬盘工具", " ", "SSDZ固态硬盘检测工具，显示SSD型号、固件版本、通电时间、写入量等信息。",    @"工具\硬盘工具\SSDZ\SSDZ.exe"),

        // 烤鸡工具
        new("FurMark",        "烤鸡工具", " ", "FurMark GPU压力测试工具，通过OpenGL高负载渲染测试显卡稳定性和散热性能，俗称烤机。",        @"工具\烤鸡工具\furmark.exe"),
        new("FurMark GUI",    "烤鸡工具", " ", "FurMark GUI版GPU烤机工具，支持自定义分辨率、抗锯齿、运行时间等参数。",        @"工具\烤鸡工具\FurMark_GUI.exe"),

        // 外设工具
        new("鼠标测试",       "外设工具", " ", "AresonMouseTest鼠标性能测试工具，测试鼠标回报率、按键响应、DPI精度、丢帧情况。",       @"工具\外设工具\AresonMouseTest\鼠标测试软件AresonMouseTestProgram.exe"),
        new("键盘测试",       "外设工具", " ", "Keyboard Test Utility键盘按键检测工具，实时显示按键状态，检测按键冲突和失灵。",       @"工具\外设工具\Keyboard Test Utility\Keyboard Test Utility.exe"),
        new("KeyTweak",       "外设工具", " ", "KeyTweak键盘按键映射工具，支持禁用、重映射键盘按键，修改键盘布局。",       @"工具\外设工具\KeyTweak\KeyTweak.exe"),
        new("MouseTester",    "外设工具", " ", "MouseTester鼠标回报率测试工具，精确测量鼠标USB回报率和移动轨迹平滑度。",     @"工具\外设工具\MouseTester\MouseTester.exe"),
        new("在线外设测试", "外设工具", " ", "在线外设测试中心，通过浏览器测试键盘按键、鼠标点击、鼠标回报率等外设性能。", @"工具\外设工具\在线外设测试中心\在线外设测试中心.bat"),

        // 显示器工具
        new("色域检测",       "显示器工具"," ", "MonitorInfo显示器色域检测工具，读取显示器EDID信息，显示色域、分辨率、刷新率等参数。",    @"工具\显示器工具\色域检测\monitorinfo.exe"),
        new("UFO显示器测试", "显示器工具"," ", "UFO Test在线显示器刷新率测试工具，通过浏览器测试显示器实际刷新率和帧率表现。", @"工具\显示器工具\UFO测试\Start.bat"),
        new("在线屏幕测试", "显示器工具"," ", "在线屏幕检测工具，通过浏览器检测显示器坏点、亮点、色彩均匀性、响应速度。",   @"工具\显示器工具\在线屏幕测试\在线屏幕测试.bat"),

        // 综合检测
        new("AIDA64",         "综合检测", " ", "AIDA64硬件综合检测工具，支持CPU/内存/显卡/硬盘全面检测和压力测试，硬件信息最全。",       @"工具\综合检测\AIDA64\aida64.exe"),
        new("HWiNFO",         "综合检测", " ", "HWiNFO硬件信息监控工具，实时显示CPU/GPU温度、频率、电压、功耗等传感器数据。",       @"工具\综合检测\hwinfo\HWiNFO64.exe"),
        new("HWMonitor",      "综合检测", " ", "HWMonitor硬件温度监控工具，显示CPU/GPU/硬盘温度、风扇转速、电压等传感器数据。",       @"工具\综合检测\HWMonitor\HWMonitor_x64.exe"),
        new("Speccy",         "综合检测", " ", "Speccy系统信息查看工具，显示操作系统、CPU、内存、主板、显卡、硬盘等详细硬件信息。",       @"工具\综合检测\speccy\Speccy64.exe"),

        // 系统工具
        new("Dism++",         "系统工具", " ", "Dism++系统优化清理工具，支持系统精简、更新清理、驱动管理、启动项管理、系统备份。",       @"工具\系统工具\Dism++\Dism++x64.exe"),
        new("DirectX Repair", "系统工具", " ", "DirectX Repair DirectX组件修复工具，修复缺失的DLL文件，解决游戏无法运行的问题。",       @"工具\系统工具\DirectX_Repair\DirectX Repair.exe"),
        new("Process Explorer","系统工具", " ", "Process Explorer高级进程管理工具，显示进程树、DLL依赖、句柄信息，比任务管理器更强大。",     @"工具\系统工具\procexp\procexp64.exe"),
        new("WinDbg",         "系统工具", " ", "WinDbg Windows调试工具，用于分析蓝屏dump文件、调试驱动程序和应用程序崩溃。",     @"工具\系统工具\WinDbg\windbg.exe"),
        new("Everything",     "系统工具", " ", "Everything极速文件搜索工具，基于NTFS索引，毫秒级搜索全盘文件，支持正则表达式。",       @"工具\系统工具\Everything\everything.exe"),
        new("FanControl",     "系统工具", " ", "FanControl开源风扇控制工具，支持根据CPU/GPU温度自定义风扇转速曲线。",       @"工具\系统工具\FanControl\FanControl.exe"),
        new("Rufus",          "系统工具", " ", "Rufus USB启动盘制作工具，支持Windows/Linux启动盘，支持UEFI/Legacy，速度极快。",      @"工具\系统工具\rufus\rufus.exe"),
        new("Ventoy",         "系统工具", " ", "Ventoy多系统启动盘制作工具，只需拷贝ISO文件即可启动，无需反复格式化U盘。",       @"工具\系统工具\ventoy\Ventoy2Disk.exe"),
        new("UltraISO",       "系统工具", " ", "UltraISO ISO镜像编辑工具，支持制作、编辑、转换ISO文件，支持直接提取引导信息。",       @"工具\系统工具\ULTRAISO\ULTRAISO.exe"),
        new("BlueScreenView", "系统工具", " ", "BlueScreenView蓝屏日志分析工具，自动读取蓝屏dump文件，显示导致蓝屏的驱动文件。",      @"工具\系统工具\bluescreenview\BlueScreenViewx64.exe"),
        new("BatteryInfo",    "系统工具", " ", "BatteryInfoView笔记本电池信息查看工具，显示电池容量、健康度、充放电次数、制造商信息。",       @"工具\系统工具\BatteryInfoView\BatteryInfoView.exe"),
        new("next.itellyou","系统工具", " ", "next.itellyou Windows原版镜像下载站，提供Windows 7~11各版本官方ISO镜像下载。", @"工具\系统工具\next_itellyou\Start.bat"),

        // 游戏平台
        new("Steam 下载",     "游戏平台", " ", "Steam游戏平台下载页面，全球最大的PC游戏平台，支持游戏购买、下载、社区功能。",     @"工具\游戏平台\Steam\下载Steam.bat"),
        new("Epic Games下载", "游戏平台", " ", "Epic Games游戏平台，每周免费游戏领取，支持虚幻引擎游戏库和社交功能。",      @"工具\游戏平台\epic\Start.bat"),
        new("EA App下载",     "游戏平台", " ", "EA App(EA桌面应用)，EA旗下游戏平台，管理EA游戏库、订阅EA Play服务。",        @"工具\游戏平台\eaapp\Start.bat"),
        new("战网下载",       "游戏平台", " ", "暴雪战网客户端，下载和管理暴雪旗下游戏，包括魔兽世界、守望先锋、暗黑破坏神等。",         @"工具\游戏平台\battle\Start.bat"),
    ];

    // ── 属性 ──────────────────────────────────────
    private string _headerTitle = "全部工具";
    public string HeaderTitle { get => _headerTitle; set { _headerTitle = value; OnPropChanged(); } }

    private string _headerSubtitle = "浏览所有便携工具";
    public string HeaderSubtitle { get => _headerSubtitle; set { _headerSubtitle = value; OnPropChanged(); } }

    private string _breadcrumb = "陈叔叔工具箱  ›  全部工具";
    public string Breadcrumb { get => _breadcrumb; set { _breadcrumb = value; OnPropChanged(); } }

    private string _statusText = "共 20 款工具  ·  v1.0";
    public string StatusText { get => _statusText; set { _statusText = value; OnPropChanged(); } }

    private string _statusTip = "";
    public string StatusTip { get => _statusTip; set { _statusTip = value; OnPropChanged(); } }

    // 选中工具描述面板
    private string _selectedToolName = "";
    public string SelectedToolName { get => _selectedToolName; set { _selectedToolName = value; OnPropChanged(); } }

    private string _selectedToolDescription = "";
    public string SelectedToolDescription { get => _selectedToolDescription; set { _selectedToolDescription = value; OnPropChanged(); } }

    private string _selectedToolCategory = "";
    public string SelectedToolCategory { get => _selectedToolCategory; set { _selectedToolCategory = value; OnPropChanged(); } }

    private System.Windows.Media.ImageSource? _selectedToolIcon;
    public System.Windows.Media.ImageSource? SelectedToolIcon { get => _selectedToolIcon; set { _selectedToolIcon = value; OnPropChanged(); } }

    private string _descriptionPanelVisible = "Collapsed";
    public string DescriptionPanelVisible { get => _descriptionPanelVisible; set { _descriptionPanelVisible = value; OnPropChanged(); } }

    private string _descDetailVisible = "Collapsed";
    public string DescDetailVisible { get => _descDetailVisible; set { _descDetailVisible = value; OnPropChanged(); } }

    private string _descPlaceholderVisible = "Visible";
    public string DescPlaceholderVisible { get => _descPlaceholderVisible; set { _descPlaceholderVisible = value; OnPropChanged(); } }

    private ToolInfo? _selectedTool;

    private string _activeCategory = "all";
    private int _prevCatIndex;
    private bool _suppressAnim;

    // ── 设置页面 ──────────────────────────────────
    private bool _isSettingsOpen;

    // ── 构造函数 ──────────────────────────────────
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        // 初始化图标缓存
        IconHelper.Init(ToolboxRoot);

        _suppressAnim = true; // 必须在 SelectedIndex 之前，抑制初始加载动画

        // 先加载删除的分类，过滤掉已删除的分类后再显示
        var deletedCategories = LoadDeletedCategories();
        Categories = Categories.Where(c => !deletedCategories.Contains(c.Id)).ToArray();
        CategoryList.ItemsSource = Categories;
        CategoryList.SelectedIndex = 0;

        // 初始化指示条
        Dispatcher.BeginInvoke(new Action(() => MoveIndicator(0, false)),
            System.Windows.Threading.DispatcherPriority.Loaded);

        // 异步加载工具（不阻塞UI）
        Loaded += (_, _) =>
        {
            // 使用 Task.Run 在后台线程加载，不阻塞 UI
            Task.Run(async () =>
            {
                await LoadToolsAsync();

                // 回到 UI 线程更新界面
                await Dispatcher.InvokeAsync(() =>
                {
                    ApplyFilter();
                });

                // 在后台线程异步加载图标
                var toolPaths = AllTools.Select(t => t.RelativePath).ToList();
                await IconHelper.LoadIconsAsync(toolPaths);

                // 图标加载完成后刷新界面
                await Dispatcher.InvokeAsync(() =>
                {
                    ApplyFilter();
                });
            });

            // 延迟2秒检查更新
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                CheckForUpdatesOnStartup();
            };
            timer.Start();
        };
    }

    // ── 异步加载工具 ──────────────────────────────
    private async Task LoadToolsAsync()
    {
        try
        {
            // 加载用户手动添加的工具
            var userTools = await ToolScanner.LoadUserToolsAsync();

            // 合并硬编码工具和用户工具
            var allTools = new List<ToolInfo>(AllTools);

            foreach (var tool in userTools)
            {
                // 避免重复
                if (!allTools.Any(t => t.RelativePath.Equals(tool.RelativePath, StringComparison.OrdinalIgnoreCase)))
                {
                    allTools.Add(tool);
                }
            }

            AllTools = allTools.ToArray();

            // 更新分类列表
            var categories = new List<CatInfo>
            {
                new("all", "全部工具", "", "浏览所有工具")
            };

            // 从工具列表中提取分类，排除已删除的分类
            var deletedCategories = LoadDeletedCategories();
            var toolCategories = AllTools
                .Select(t => t.Category)
                .Distinct()
                .Where(c => !deletedCategories.Contains(c))
                .OrderBy(c => c);

            foreach (var cat in toolCategories)
            {
                // 查找分类图标
                var existingCat = Categories.FirstOrDefault(c => c.Id == cat);
                if (existingCat != null)
                {
                    categories.Add(existingCat);
                }
                else
                {
                    categories.Add(new CatInfo(cat, cat, " ", cat));
                }
            }

            // 按照指定顺序排序分类
            var categoryOrder = new List<string>
            {
                "全部工具", "娱乐工具", "实用工具", "搞机工具", "文件工具", "清理工具",
                "依赖", "CPU工具", "内存工具", "显卡工具", "硬盘工具", "烤鸡工具",
                "外设工具", "显示器工具", "系统工具", "游戏平台"
            };

            categories.Sort((a, b) =>
            {
                var indexA = categoryOrder.IndexOf(a.Name);
                var indexB = categoryOrder.IndexOf(b.Name);
                if (indexA == -1) indexA = 100;
                if (indexB == -1) indexB = 100;
                return indexA.CompareTo(indexB);
            });

            Categories = categories.ToArray();
            CategoryList.ItemsSource = Categories;
            CategoryList.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            Logger.Log($"加载工具失败: {ex.Message}");
        }
    }

    // ── 导航按钮线性高亮动画 ──────────────────────
    private void NavItem_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is ListBoxItem { IsLoaded: true } item)
        {
            var bd = FindVisualChild<Border>(item, "Bd");
            if (bd != null && !item.IsSelected)
                AnimateBg(bd, "#0fffffff", 0.12);
        }
    }

    private void NavItem_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is ListBoxItem { IsLoaded: true } item)
        {
            var bd = FindVisualChild<Border>(item, "Bd");
            if (bd != null)
            {
                var target = item.IsSelected ? "#18ffffff" : "Transparent";
                AnimateBg(bd, target, 0.12);
            }
        }
    }

    // ── 右键菜单：删除分类 ──────────────────────────
    // ── Win11 风格右键菜单 ──────────────────────────
    private static Window? _activeMenu;
    private static bool _isMenuClosing;

    private void NavItem_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBoxItem item) return;
        if (item.DataContext is not CatInfo cat) return;
        if (cat.Id == "all") return; // 不允许删除"全部工具"

        e.Handled = true;
        ShowCategoryMenu(cat, e);
    }

    private void ShowCategoryMenu(CatInfo cat, MouseButtonEventArgs e)
    {
        // 关闭已有菜单
        _activeMenu?.Close();
        _activeMenu = null;

        // 计算位置
        var screenPos = PointToScreen(e.GetPosition(this));

        // 创建菜单窗口
        var menu = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = false,
            Background = new SolidColorBrush(Color.FromRgb(0x2b, 0x2b, 0x2b)),
            Width = 224,
            SizeToContent = SizeToContent.Height,
            ShowInTaskbar = false,
            Topmost = true,
            Left = screenPos.X + 8,
            Top = screenPos.Y - 10,
            ResizeMode = ResizeMode.NoResize,
        };

        // 加载后设置圆角区域
        menu.Loaded += (_, _) =>
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(menu);
            int width = (int)menu.ActualWidth;
            int height = (int)menu.ActualHeight;
            var hRgn = CreateRoundRectRgn(0, 0, width + 1, height + 1, 16, 16);
            SetWindowRgn(helper.Handle, hRgn, true);
            DeleteObject(hRgn);
        };

        // 主容器
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x2b, 0x2b, 0x2b)),
            CornerRadius = new CornerRadius(8),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xff, 0xff, 0xff)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0),
        };

        var stack = new StackPanel { Margin = new Thickness(4) };

        // 关闭菜单方法
        void CloseMenuWithAnimation(Action? onClosed = null)
        {
            if (_isMenuClosing) return;
            _isMenuClosing = true;

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(100));
            fadeOut.Completed += (_, _) =>
            {
                menu.Close();
                _activeMenu = null;
                _isMenuClosing = false;
                onClosed?.Invoke();
            };
            menu.BeginAnimation(Window.OpacityProperty, fadeOut);
        }

        // 删除分类菜单项
        stack.Children.Add(CreateMenuItem(
            "M 3 3 L 5 3 L 5 1 L 11 1 L 11 3 L 13 3 L 13 5 L 3 5 Z M 4 5 L 12 5 L 11 15 L 5 15 Z",
            "删除分类", () =>
        {
            CloseMenuWithAnimation(() => DeleteCategory(cat));
        }, isDestructive: true));

        border.Child = stack;
        menu.Content = border;
        menu.Opacity = 0;

        // 失去焦点时关闭
        menu.Deactivated += (_, _) => CloseMenuWithAnimation();
        menu.Closed += (_, _) =>
        {
            if (_activeMenu == menu) _activeMenu = null;
        };

        menu.Show();
        _activeMenu = menu;

        // 确保不超出屏幕
        menu.UpdateLayout();
        var workArea = SystemParameters.WorkArea;
        if (menu.Left + menu.Width > workArea.Right)
            menu.Left = screenPos.X - menu.Width - 8;
        if (menu.Top + menu.Height > workArea.Bottom)
            menu.Top = workArea.Bottom - menu.Height - 8;

        // 淡入动画
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 2 }
        };
        menu.BeginAnimation(Window.OpacityProperty, fadeIn);
    }

    private static Border CreateMenuItem(string pathData, string text, Action onClick, bool isDestructive = false)
    {
        var bgBrush = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x2d));
        var item = new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 8, 10, 8),
            Cursor = Cursors.Hand,
            Background = bgBrush,
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var iconPath = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse(pathData),
            Fill = new SolidColorBrush(isDestructive ? Color.FromRgb(0xff, 0x6b, 0x6b) : Color.FromRgb(0xaa, 0xaa, 0xaa)),
            Width = 14,
            Height = 14,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        Grid.SetColumn(iconPath, 0);

        var nameText = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily("Microsoft YaHei"),
            FontSize = 12,
            Foreground = isDestructive ? new SolidColorBrush(Color.FromRgb(0xff, 0x6b, 0x6b)) : Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
        };
        Grid.SetColumn(nameText, 1);

        grid.Children.Add(iconPath);
        grid.Children.Add(nameText);
        item.Child = grid;

        var hoverColor = Color.FromRgb(0x35, 0x35, 0x35);
        var normalColor = Color.FromRgb(0x2d, 0x2d, 0x2d);

        item.MouseEnter += (_, _) =>
        {
            var anim = new ColorAnimation(hoverColor, TimeSpan.FromMilliseconds(150));
            bgBrush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
        };
        item.MouseLeave += (_, _) =>
        {
            var anim = new ColorAnimation(normalColor, TimeSpan.FromMilliseconds(150));
            bgBrush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
        };
        item.PreviewMouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            onClick();
        };

        return item;
    }

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);



    private void DeleteCategory(CatInfo cat)
    {
        var categoryDir = Path.Combine(ToolboxRoot, "工具", cat.Name);
        bool hasTools = Directory.Exists(categoryDir) && Directory.GetFiles(categoryDir, "*", SearchOption.AllDirectories).Length > 0;

        string message;
        if (hasTools)
        {
            message = $"分类 \"{cat.Name}\" 包含工具文件。\n\n删除此分类将永久删除该分类下的所有工具文件，且无法撤销。\n\n确定要继续吗？";
        }
        else
        {
            message = $"确定要删除空分类 \"{cat.Name}\" 吗？";
        }

        var result = CustomMessageBox.Show(message, "确认删除", MessageBoxButton.YesNo);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            // 删除目录（如果存在）
            if (Directory.Exists(categoryDir))
            {
                Directory.Delete(categoryDir, true);
            }

            // 从分类列表中移除
            var list = Categories.ToList();
            list.Remove(cat);
            Categories = list.ToArray();
            CategoryList.ItemsSource = Categories;

            // 保存已删除的分类到配置文件
            SaveDeletedCategories(cat.Name);

            // 如果删除的是当前选中的分类，切换到"全部工具"
            if (_activeCategory == cat.Id)
            {
                CategoryList.SelectedIndex = 0;
            }

            ApplyFilter();
            Logger.Log($"删除分类: {cat.Name}");
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"删除失败: {ex.Message}", "错误");
            Logger.Log($"删除分类失败: {ex.Message}");
        }
    }

    private void SaveDeletedCategories(string categoryName)
    {
        var configPath = Path.Combine(ToolboxRoot, "deleted_categories.json");
        var deleted = new List<string>();

        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                deleted = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch { }
        }

        if (!deleted.Contains(categoryName))
        {
            deleted.Add(categoryName);
            File.WriteAllText(configPath, System.Text.Json.JsonSerializer.Serialize(deleted));
        }
    }

    private List<string> LoadDeletedCategories()
    {
        var configPath = Path.Combine(ToolboxRoot, "deleted_categories.json");
        if (!File.Exists(configPath)) return new List<string>();

        try
        {
            var json = File.ReadAllText(configPath);
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private void ResetNavItemBackgrounds()
    {
        // 强制所有导航项回到正确背景色（覆盖残留动画）
        for (int i = 0; i < CategoryList.Items.Count; i++)
        {
            if (CategoryList.ItemContainerGenerator.ContainerFromIndex(i) is ListBoxItem item)
            {
                var bd = FindVisualChild<Border>(item, "Bd");
                if (bd != null)
                {
                    bd.Background = new System.Windows.Media.SolidColorBrush(
                        item.IsSelected
                            ? (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#18ffffff")
                            : System.Windows.Media.Colors.Transparent);
                }
            }
        }
    }

    private static T? FindVisualChild<T>(System.Windows.DependencyObject parent, string name, int depth = 0)
        where T : System.Windows.FrameworkElement
    {
        if (depth > 6) return null; // 模板嵌套不会超过 6 层
        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count && i < 32; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T elem && elem.Name == name) return elem;
            var found = FindVisualChild<T>(child, name, depth + 1);
            if (found != null) return found;
        }
        return null;
    }

    private static void AnimateBg(Border bd, string toHex, double seconds)
    {
        var to = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(toHex);
        var anim = new System.Windows.Media.Animation.ColorAnimation(
            to, TimeSpan.FromSeconds(seconds))
        {
            EasingFunction = new System.Windows.Media.Animation.PowerEase
            {
                EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut,
                Power = 1.5
            }
        };
        // 克隆为新画刷，避免 Trigger 设置的 Frozen 画刷无法动画
        var from = bd.Background is System.Windows.Media.SolidColorBrush scb
            ? scb.Color : System.Windows.Media.Colors.Transparent;
        var brush = new System.Windows.Media.SolidColorBrush(from);
        bd.Background = brush;
        brush.BeginAnimation(System.Windows.Media.SolidColorBrush.ColorProperty, anim);
    }

    // ── 自定义标题栏 ──────────────────────────────
    private void TitleBar_Drag(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) { OnMaximize(null!, null!); return; }
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            DragMove();
    }

    private void OnMinimize(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void OnMaximize(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            BtnMax.Content = "";
        }
        else
        {
            WindowState = WindowState.Maximized;
            BtnMax.Content = "";
        }
    }

    // ── 添加工具 ──────────────────────────────────
    private void OnAddTool(object sender, RoutedEventArgs e)
    {
        // 获取已有分类列表
        var existingCategories = Categories
            .Where(c => c.Id != "all")
            .Select(c => c.Name)
            .ToArray();

        var dialog = new AddToolDialog(ToolboxRoot, existingCategories)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                // 创建新工具信息
                var newTool = new ToolInfo(
                    Name: dialog.ToolName!,
                    Category: dialog.CategoryName!,
                    Icon: " ",
                    Description: dialog.ToolDescription ?? "",
                    RelativePath: dialog.SourcePath!
                );

                // 添加到工具列表
                var toolsList = AllTools.ToList();
                toolsList.Add(newTool);
                AllTools = toolsList.ToArray();

                // 添加到用户工具配置
                ToolScanner.AddTool(newTool);

                // 更新分类列表（如果需要）
                var existingCat = Categories.FirstOrDefault(c => c.Id == dialog.CategoryName);
                if (existingCat == null)
                {
                    var categoriesList = Categories.ToList();
                    categoriesList.Add(new CatInfo(dialog.CategoryName!, dialog.CategoryName!, " ", dialog.CategoryName!));
                    Categories = categoriesList.ToArray();
                    CategoryList.ItemsSource = Categories;
                }

                // 刷新界面
                ApplyFilter();
                StatusTip = $"已添加工具: {dialog.ToolName}";
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"添加工具失败: {ex.Message}", "错误");
            }
        }
    }

    /// <summary>
    /// 递归复制目录
    /// </summary>
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(destDir, fileName);
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            var destSubDir = Path.Combine(destDir, dirName);
            CopyDirectory(dir, destSubDir);
        }
    }

    private void OnClose(object sender, RoutedEventArgs e)
        => Close();

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        BtnMax.Content = WindowState == WindowState.Maximized ? "" : "";
    }

    // ── 分类切换 ──────────────────────────────────
    private void OnCategoryChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryList.SelectedItem is CatInfo cat)
        {
            var cats = Categories;
            var newIndex = Array.FindIndex(cats, c => c.Id == cat.Id);
            var oldIndex = _prevCatIndex;
            _prevCatIndex = newIndex;

            _activeCategory = cat.Id;

            if (!_suppressAnim && oldIndex != newIndex)
            {
                var dir = newIndex > oldIndex ? 1 : -1;
                // 传入分类信息，在动画回调中设置文字
                AnimatePageTransition(dir, cat.Name, cat.Subtitle);
                MoveIndicator(newIndex, true);
            }
            else
            {
                _suppressAnim = false;
                HeaderTitle = cat.Name;
                HeaderSubtitle = cat.Subtitle;
                Breadcrumb = $"陈叔叔工具箱  ›  {cat.Name}";
                ApplyFilter();
                MoveIndicator(newIndex, false);
            }
            Dispatcher.BeginInvoke(new Action(ResetNavItemBackgrounds),
                System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    private void AnimatePageTransition(int direction, string catName, string catSubtitle)
    {
        // Phase 1: 淡出
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new PowerEase { EasingMode = EasingMode.EaseIn, Power = 1.5 }
        };
        fadeOut.Completed += (_, _) =>
        {
            // 切换文字和内容（此时不可见）
            HeaderTitle = catName;
            HeaderSubtitle = catSubtitle;
            Breadcrumb = $"陈叔叔工具箱  ›  {catName}";
            ApplyFilter();
            ContentGrid.UpdateLayout();

            // Phase 2: 滑入 + 淡入 同时播放
            var tt = new TranslateTransform();
            ContentGrid.RenderTransform = tt;
            tt.Y = direction * 50;
            ContentGrid.Opacity = 0;
            ContentGrid.UpdateLayout();

            var sb = new Storyboard();

            var slide = new DoubleAnimation(tt.Y, 0, TimeSpan.FromMilliseconds(380))
            {
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 2.5 }
            };
            Storyboard.SetTarget(slide, ContentGrid);
            Storyboard.SetTargetProperty(slide,
                new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
            sb.Children.Add(slide);

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
            {
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 1.5 }
            };
            Storyboard.SetTarget(fadeIn, ContentGrid);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
            sb.Children.Add(fadeIn);

            sb.Begin();
        };

        ContentGrid.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }

    // ── 指示条动画 ────────────────────────────────
    private int _prevSelectedIndex = -1;

    private void MoveIndicator(int newIndex, bool animate)
    {
        // 恢复旧项指示条
        if (_prevSelectedIndex >= 0 && _prevSelectedIndex < CategoryList.Items.Count)
        {
            if (CategoryList.ItemContainerGenerator.ContainerFromIndex(_prevSelectedIndex) is ListBoxItem oldItem)
            {
                var oldInd = FindVisualChild<Border>(oldItem, "Indicator");
                if (oldInd != null)
                    AnimateIndicatorHeight(oldInd, 0, animate ? 200 : 0);
            }
        }

        // 展开新项指示条
        if (newIndex >= 0 && newIndex < CategoryList.Items.Count)
        {
            if (CategoryList.ItemContainerGenerator.ContainerFromIndex(newIndex) is ListBoxItem newItem)
            {
                var newInd = FindVisualChild<Border>(newItem, "Indicator");
                if (newInd != null)
                    AnimateIndicatorHeight(newInd, 18, animate ? 380 : 0);
            }
        }

        _prevSelectedIndex = newIndex;
    }

    private void AnimateIndicatorHeight(Border indicator, double toHeight, int durationMs)
    {
        if (durationMs <= 0)
        {
            indicator.Height = toHeight;
            return;
        }
        var anim = new System.Windows.Media.Animation.DoubleAnimation(
            toHeight, TimeSpan.FromMilliseconds(durationMs))
        {
            EasingFunction = new System.Windows.Media.Animation.PowerEase
            {
                EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut,
                Power = 2.5
            }
        };
        indicator.BeginAnimation(Border.HeightProperty, anim);
    }

    // ── 设置页面动画 ──────────────────────────────
    private void OnSettings(object sender, RoutedEventArgs e)
    {
        if (_isSettingsOpen) return;
        ShowSettingsPanel();
    }

    private void ShowSettingsPanel()
    {
        _isSettingsOpen = true;
        SettingsOverlay.Visibility = Visibility.Visible;

        // 初始状态：从右侧偏移，透明
        var tt = new TranslateTransform(300, 0);
        SettingsPanelCtrl.RenderTransform = tt;
        SettingsOverlay.Opacity = 0;
        SettingsPanelCtrl.UpdateLayout();

        var sb = new Storyboard();

        // 滑入动画（从右向左）
        var slide = new DoubleAnimation(300, 0, TimeSpan.FromMilliseconds(380))
        {
            EasingFunction = new PowerEase
            {
                EasingMode = EasingMode.EaseOut,
                Power = 2.5
            }
        };
        Storyboard.SetTarget(slide, SettingsPanelCtrl);
        Storyboard.SetTargetProperty(slide,
            new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
        sb.Children.Add(slide);

        // 渐显动画
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new PowerEase
            {
                EasingMode = EasingMode.EaseOut,
                Power = 1.5
            }
        };
        Storyboard.SetTarget(fade, SettingsOverlay);
        Storyboard.SetTargetProperty(fade, new PropertyPath(UIElement.OpacityProperty));
        sb.Children.Add(fade);

        sb.Begin();
    }

    private void HideSettingsPanel()
    {
        var sb = new Storyboard();
        sb.Completed += (_, _) =>
        {
            SettingsOverlay.Visibility = Visibility.Collapsed;
            _isSettingsOpen = false;
        };

        // 滑出动画（从左向右）
        var slide = new DoubleAnimation(0, 300, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new PowerEase
            {
                EasingMode = EasingMode.EaseIn,
                Power = 2.5
            }
        };
        Storyboard.SetTarget(slide, SettingsPanelCtrl);
        Storyboard.SetTargetProperty(slide,
            new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
        sb.Children.Add(slide);

        // 渐隐动画
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new PowerEase
            {
                EasingMode = EasingMode.EaseIn,
                Power = 1.5
            }
        };
        Storyboard.SetTarget(fade, SettingsOverlay);
        Storyboard.SetTargetProperty(fade, new PropertyPath(UIElement.OpacityProperty));
        sb.Children.Add(fade);

        sb.Begin();
    }

    private void OnSettingsOverlayClick(object sender, MouseButtonEventArgs e)
    {
        // 点击遮罩层关闭设置页面
        if (e.OriginalSource == sender)
        {
            HideSettingsPanel();
        }
    }

    private void OnSettingsCloseRequested(object? sender, EventArgs e)
    {
        HideSettingsPanel();
    }

    // ── 启动时检查更新 ────────────────────────────
    private string? _releaseUrl;

    private async void CheckForUpdatesOnStartup()
    {
        try
        {
            // 检查是否启用自动更新
            var configPath = Path.Combine(ToolboxRoot, "settings.json");
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                if (settings != null && settings.TryGetValue("autoCheckUpdate", out var autoCheck) && !autoCheck)
                    return;
            }

            var checker = new UpdateChecker("Foxelf-Studio", "AngkleChen-ToolBox");
            var result = await checker.CheckForUpdatesAsync();

            if (result.HasUpdate)
            {
                _releaseUrl = result.ReleaseUrl;
                ShowUpdateToast();
            }
        }
        catch
        {
            // 静默失败
        }
    }

    private void ShowUpdateToast()
    {
        UpdateToastPanel.Visibility = Visibility.Visible;
        UpdateToastPanel.Opacity = 0;

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 2 }
        };
        UpdateToastPanel.BeginAnimation(OpacityProperty, fadeIn);
    }

    private void OnToastCloseClick(object sender, RoutedEventArgs e)
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
        fadeOut.Completed += (_, _) => UpdateToastPanel.Visibility = Visibility.Collapsed;
        UpdateToastPanel.BeginAnimation(OpacityProperty, fadeOut);
    }

    private void OnToastViewClick(object sender, RoutedEventArgs e)
    {
        if (_releaseUrl != null)
        {
            Process.Start(new ProcessStartInfo(_releaseUrl) { UseShellExecute = true });
        }
        UpdateToastPanel.Visibility = Visibility.Collapsed;
    }

    // ── 搜索 ──────────────────────────────────────
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var query = SearchBox.Text.Trim();
        var filtered = AllTools.AsEnumerable();

        // 过滤掉文件不存在的工具
        filtered = filtered.Where(t =>
        {
            var path = System.IO.Path.Combine(ToolboxRoot, t.RelativePath);
            return System.IO.File.Exists(path) || System.IO.Directory.Exists(path);
        });

        if (_activeCategory != "all")
            filtered = filtered.Where(t => t.Category == _activeCategory);

        if (!string.IsNullOrEmpty(query))
        {
            var q = query.ToLower();
            filtered = filtered.Where(t =>
                t.Name.ToLower().Contains(q) ||
                t.Description.ToLower().Contains(q) ||
                t.Category.ToLower().Contains(q));
        }

        // 按照导航栏分类顺序排序
        var categoryOrder = Categories.Select(c => c.Name).ToList();
        var list = filtered.OrderBy(t =>
        {
            var index = categoryOrder.IndexOf(t.Category);
            return index == -1 ? 999 : index;
        }).ThenBy(t => t.Name).ToList();

        CardItems.ItemsSource = list;
        StatusText = $"共 {list.Count} 款工具  ·  v1.1";
    }

    // ── 工具选中（单击） ─────────────────────────
    public ICommand SelectCommand => new RelayCommand(tool =>
    {
        if (tool is ToolInfo t)
        {
            _selectedTool = t;
            SelectedToolName = t.Name;
            SelectedToolDescription = string.IsNullOrEmpty(t.Detail) ? t.Description : t.Detail;
            SelectedToolCategory = t.Category;
            SelectedToolIcon = IconHelper.GetIcon(t.RelativePath);
            DescDetailVisible = "Visible";
            DescPlaceholderVisible = "Collapsed";
        }
    });

    // ── 工具启动（双击） ──────────────────────────
    public ICommand LaunchCommand => new RelayCommand(tool =>
    {
        if (tool is ToolInfo t)
        {
            var path = System.IO.Path.Combine(ToolboxRoot, t.RelativePath);
            if (!System.IO.Path.Exists(path))
            {
                CustomMessageBox.Show($"文件不存在:\n{path}", "提示");
                return;
            }
            try
            {
                var ext = System.IO.Path.GetExtension(path).ToLower();
                var workDir = System.IO.Path.GetDirectoryName(path)!;

                var psi = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = workDir,
                };

                if (ext is ".exe" or ".msi")
                {
                    psi.FileName = path;
                    Process.Start(psi);
                }
                else if (ext is ".bat" or ".cmd")
                {
                    psi.FileName = "cmd.exe";
                    psi.Arguments = $"/c \"{path}\"";
                    Process.Start(psi);
                }
                else if (System.IO.Directory.Exists(path))
                {
                    Process.Start("explorer.exe", $"\"{path}\"");
                }
                else
                {
                    psi.FileName = path;
                    Process.Start(psi);
                }
                StatusTip = $"已启动 {t.Name}";
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"启动失败:\n{ex.Message}", "提示");
            }
        }
    });

    // ── 删除工具 ──────────────────────────────────
    public ICommand DeleteCommand => new RelayCommand(tool =>
    {
        if (tool is not ToolInfo t) return;

        var path = Path.Combine(ToolboxRoot, t.RelativePath);

        try
        {
            // 确定要删除的目标（如果是文件，删除其父文件夹）
            string? targetDir = null;

            if (File.Exists(path))
            {
                targetDir = Path.GetDirectoryName(path);
            }
            else if (Directory.Exists(path))
            {
                targetDir = path;
            }
            else
            {
                CustomMessageBox.Show("文件不存在，可能已被删除。", "提示");
            }

            // 删除整个文件夹
            if (targetDir != null && Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
                Logger.Log($"删除文件夹: {targetDir}");
            }

            // 从 AllTools 中移除该工具（避免重复显示）
            var toolsList = AllTools.ToList();
            toolsList.RemoveAll(x => x.RelativePath == t.RelativePath);
            AllTools = toolsList.ToArray();

            // 刷新工具列表
            ApplyFilter();

            StatusTip = $"已删除 {t.Name}";
            Logger.Log($"删除工具: {t.Name}, 路径: {path}");
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"删除失败: {ex.Message}", "错误");
            Logger.Log($"删除工具失败: {ex.Message}");
        }
    });

    // ── INotifyPropertyChanged ────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// ── 轻量 ICommand ────────────────────────────────
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    public RelayCommand(Action<object?> execute) => _execute = execute;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged { add { } remove { } }
}
