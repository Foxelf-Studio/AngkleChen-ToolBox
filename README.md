<div align="center">

# 🔧 陈叔叔工具箱

**一款专为电脑维修、系统调试、硬件检测打造的便携式工具集合**

![Version](https://img.shields.io/badge/version-2.1.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)

</div>

## 📖 简介

陈叔叔工具箱是一款集成了 80+ 款常用系统工具的便携式工具箱应用程序。专为电脑维修技术人员、系统管理员和普通用户设计，提供一站式硬件检测、系统维护、软件安装解决方案。

**无需安装，双击即用，不写注册表，不产生系统垃圾。**

## ✨ 功能特点

- 🚀 **便携免安装** - U盘随身携带，即开即用
- 🎨 **现代化界面** - Win11 风格 Fluent Design UI，深色主题护眼
- 🔍 **智能搜索** - 快速查找所需工具，支持模糊搜索
- 📦 **分类管理** - 17 个工具分类，80+ 款工具一目了然
- 🔄 **更新检测** - 自动检查新版本，用户纯手动下载替换
- 🛠️ **扩展支持** - 可添加自定义工具，灵活扩展功能

## 🗂️ 工具分类

| 分类 | 图标 | 工具数量 | 主要工具 |
|------|------|----------|----------|
| 🎵 娱乐工具 | 🎮 | 3 | 酷我音乐、PiliPlus、洛雪音乐 |
| 💼 实用工具 | ⚙️ | 9 | Edge浏览器、Office Tool、WPS、网卡驱动 |
| 🔧 搞机工具 | 🛠️ | 5 | HEU KMS、沧水KMS、ADB工具、鲁大师 |
| 📁 文件工具 | 📂 | 5 | 图压、磁盘精灵、迅雷、格式转换 |
| 🧹 清理工具 | 🗑️ | 4 | HiBit Uninstaller、IObit、Geek、SoftCnKiller |
| 📦 依赖 | 📚 | 4 | .NET Framework、VC++ 运行库 |
| 🖥️ CPU工具 | 💻 | 7 | CPU-Z、Core Temp、ThrottleStop、Prime95 |
| 💾 内存工具 | 🧠 | 5 | MemTest、Thaiphoon、TM5、ZenTimings |
| 🎮 显卡工具 | 🖥️ | 6 | GPU-Z、DDU、nvidiaInspector、ATITool |
| 💿 硬盘工具 | 💽 | 5 | CrystalDiskInfo、WizTree、HD Tune |
| 🔥 烤鸡工具 | 🔥 | 4 | FurMark、AIDA64、Prime95、LinX |
| ⌨️ 外设工具 | 🖱️ | 3 | 鼠标测试、键盘测试、手柄测试 |
| 🖵 显示器工具 | 🖥️ | 2 | 色域检测、屏幕校准 |
| 📊 综合检测 | 📈 | 4 | AIDA64、HWiNFO、Speccy、SiSoftware |
| ⚙️ 系统工具 | 🔧 | 8 | Dism++、Everything、Rufus、CCleaner |
| 🎮 游戏平台 | 🎮 | 5 | Steam、Epic、EA App、WeGame、Uplay |

## 📥 下载与使用

1. 从 [GitHub Releases](https://github.com/Foxelf-Studio/AngkleChen-ToolBox/releases) 的更新日志最下方找到网盘链接下载压缩包
2. 解压到任意目录
3. 双击 `AngkleChenToolBox.exe` 即可启动

### 系统要求

- **操作系统**：Windows 10 1809 或更高版本 / Windows 11
- **架构**：x64（64位）
- **运行时**：.NET 10.0（已内置，无需额外安装）

### 使用方法

1. **浏览工具**：在左侧导航栏选择工具分类
2. **搜索工具**：使用顶部搜索框快速查找
3. **查看详情**：点击工具卡片查看详细描述
4. **启动工具**：双击工具卡片即可启动对应程序
5. **添加工具**：点击标题栏 "+" 按钮添加自定义工具

## 🔄 更新

若有更新，自行前往[GitHub Releases](https://github.com/Foxelf-Studio/AngkleChen-ToolBox/releases)下载最新程序替换。

## 🛠️ 开发技术

- **框架**：.NET 10.0 + WPF
- **语言**：C# 12
- **UI 风格**：Win11 Fluent Design
- **图标提取**：Windows Shell API + 缓存机制
- **配置管理**：JSON 配置文件 + 动态加载

## 📁 项目结构

```
陈叔叔工具箱/
├── Controls/               # 自定义控件
│   ├── ToolCard.xaml       # 工具卡片组件
│   ├── SettingsPanel.xaml  # 设置面板
│   ├── UpdateDialog.xaml   # 更新对话框
│   ├── AddToolDialog.xaml  # 添加工具对话框
│   └── ProgressDialog.xaml # 进度对话框
├── Helpers/                # 辅助类
│   ├── IconHelper.cs       # 图标提取与缓存
│   ├── UpdateChecker.cs    # 更新检查器
│   ├── AppConfig.cs        # 配置管理
│   └── Logger.cs           # 日志记录
├── Models/                 # 数据模型
│   ├── ToolInfo.cs         # 工具信息模型
│   └── CatInfo.cs          # 分类信息模型
├── 工具/                   # 内置工具目录
├── 扩展工具/               # 扩展工具（可选）
├── Result/                 # 便携版输出目录
├── 发布脚本/               # 发布相关脚本
├── config.json             # 工具配置文件
├── MainWindow.xaml         # 主窗口 XAML
├── MainWindow.xaml.cs      # 主窗口逻辑
└── App.xaml                # 应用程序入口
```

## ⚙️ 配置说明

### config.json 结构

```json
{
  "categories": [
    {
      "name": "分类名称",
      "icon": "图标代码",
      "description": "分类描述",
      "tools": [
        {
          "name": "工具名称",
          "description": "工具描述",
          "relativePath": "工具相对路径"
        }
      ]
    }
  ]
}
```

### 添加自定义工具

1. 程序标题栏上点击“+”号
2. 填写相关信息
3. 程序自动在 `config.json` 中添加工具配置
4. 重启程序即可看到新工具

## 🔧 开发与构建

### 环境要求

- Visual Studio 2022 或更高版本
- .NET 10.0 SDK
- Windows 10/11 开发环境

### 构建步骤

```bash
# 克隆仓库
git clone https://github.com/Foxelf-Studio/AngkleChen-ToolBox.git

# 进入项目目录
cd AngkleChen-ToolBox

# 编译项目
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# 输出文件位于 publish/ 目录
```


## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！

1. Fork 本仓库
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 📄 许可证

本项目采用 [MIT 许可证](LICENSE) 开源。

## 🙏 致谢

感谢以下开源项目和工具：

- [CPU-Z](https://www.cpuid.com/softwares/cpu-z.html) - CPU 检测工具
- [GPU-Z](https://www.techpowerup.com/gpuz/) - GPU 检测工具
- [CrystalDiskInfo](https://crystalmark.info/en/software/crystaldiskinfo/) - 硬盘检测工具
- [Dism++](https://www.chuyu.me/) - 系统优化工具
- [Everything](https://www.voidtools.com/) - 文件搜索工具
- [FurMark](https://www.geeks3d.com/furmark/) - GPU 压力测试
- [AIDA64](https://www.aida64.com/) - 硬件检测工具
- 以及所有内置工具的开发者

## 📮 联系方式

- **GitHub**：[Foxelf-Studio/AngkleChen-ToolBox](https://github.com/Foxelf-Studio/AngkleChen-ToolBox)
- **Issues**：[提交问题](https://github.com/Foxelf-Studio/AngkleChen-ToolBox/issues)
- **Discussions**：[参与讨论](https://github.com/Foxelf-Studio/AngkleChen-ToolBox/discussions)
- **邮箱** SeenyDafa@gmail.com

---

<div align="center">

**⭐ 如果觉得有用，请给个 Star 支持一下！⭐**

**🔧 陈叔叔工具箱 - 让电脑维修更简单 🔧**

</div>
