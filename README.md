<div align="center">

# 🔧 陈叔叔工具箱

**一款集合常用系统工具的便携式工具箱**

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

</div>

## 📖 简介

陈叔叔工具箱是一款专为电脑维修、系统调试、硬件检测打造的便携式工具集合。集成 60+ 款常用工具，无需安装，即开即用。

## ✨ 功能特点

- 🚀 **便携免安装** - 单文件运行，不写注册表，不产生垃圾
- 🎨 **现代化界面** - Win11 风格 UI，支持深色主题
- 🔍 **智能搜索** - 快速查找所需工具
- 📦 **分类管理** - 工具按功能分类，一目了然
- 🔄 **在线更新** - 支持增量更新，无需重复下载
- 🛠️ **扩展支持** - 可下载扩展工具包获取更多工具

## 🗂️ 工具分类

| 分类 | 工具示例 |
|------|----------|
| 🎮 娱乐工具 | 酷我音乐、PiliPlus |
| 💼 实用工具 | Edge浏览器、Office Tool、WPS |
| 🔧 搞机工具 | HEU KMS、鲁大师、ADB工具 |
| 📁 文件工具 | 磁盘精灵、迅雷、格式转换 |
| 🧹 清理工具 | HiBit Uninstaller、SoftCnKiller |
| 📦 依赖 | .NET Framework、VC++ 运行库 |
| 🖥️ CPU工具 | CPU-Z、Core Temp、Prime95 |
| 💾 内存工具 | MemTest、Thaiphoon、TM5 |
| 🎮 显卡工具 | GPU-Z、DDU、nvidiaInspector |
| 💿 硬盘工具 | CrystalDiskInfo、WizTree |
| 🔥 烤鸡工具 | FurMark |
| ⌨️ 外设工具 | 鼠标测试、键盘测试 |
| 🖵 显示器工具 | 色域检测 |
| 📊 综合检测 | AIDA64、HWiNFO |
| ⚙️ 系统工具 | Dism++、Everything、Rufus |
| 🎮 游戏平台 | Steam、Epic、EA App |

## 📥 下载

### 便携版（推荐）

下载 `陈叔叔工具箱.exe` 即可使用，无需安装。

### 安装版

下载 `陈叔叔工具箱-安装版-Lite.exe` 或 `陈叔叔工具箱-安装版-Full.exe` 进行安装。

- **Lite 版**：仅包含主程序和基础工具
- **Full 版**：包含所有扩展工具

## 🚀 使用方法

1. 下载并运行 `陈叔叔工具箱.exe`
2. 在左侧导航栏选择工具分类
3. 点击工具卡片查看详情
4. 双击工具卡片即可启动

## 🔄 更新机制

程序支持通过 GitHub Release 进行增量更新：

1. 启动时自动检查更新（可在设置中关闭）
2. 发现新版本后，只下载变化的文件
3. 无需重复下载完整安装包

## 🛠️ 开发技术

- **框架**：.NET 10 + WPF
- **语言**：C#
- **UI 风格**：Win11 Fluent Design
- **更新机制**：GitHub API + 文件级增量更新

## 📁 项目结构

```
陈叔叔工具箱/
├── Controls/           # 自定义控件
│   ├── ToolCard.xaml   # 工具卡片
│   ├── SettingsPanel.xaml  # 设置页面
│   └── UpdateDialog.xaml   # 更新对话框
├── Helpers/            # 辅助类
│   ├── IconHelper.cs   # 图标提取
│   ├── UpdateChecker.cs # 更新检查
│   └── VersionInfo.cs  # 版本信息模型
├── Models/             # 数据模型
├── 工具/               # 内置工具
├── 扩展工具/           # 扩展工具（可选）
├── Result/             # 便携版输出
└── 发布/               # 安装版输出
```

## 📋 系统要求

- Windows 10 1809 或更高版本
- Windows 11
- x64 架构

## 📄 许可证

本项目采用 [MIT 许可证](LICENSE) 开源。

## 🙏 致谢

感谢以下开源项目：

- [CPU-Z](https://www.cpuid.com/softwares/cpu-z.html)
- [GPU-Z](https://www.techpowerup.com/gpuz/)
- [CrystalDiskInfo](https://crystalmark.info/en/software/crystaldiskinfo/)
- [Dism++](https://www.chuyu.me/)
- [Everything](https://www.voidtools.com/)
- 以及所有内置工具的开发者

## 📮 联系方式

- GitHub：[Foxelf-Studio/AngkleChen-ToolBox](https://github.com/Foxelf-Studio/AngkleChen-ToolBox)

---

<div align="center">

**⭐ 如果觉得有用，请给个 Star 支持一下！⭐**

</div>
