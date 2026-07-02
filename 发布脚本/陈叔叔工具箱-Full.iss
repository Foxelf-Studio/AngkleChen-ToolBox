; ============================================
; 陈叔叔工具箱 完整版 - Inno Setup 安装脚本
; ============================================

#define MyAppName "陈叔叔工具箱"
#define MyAppVersion "1.2.0"
#define MyAppPublisher "陈叔叔"
#define MyAppExeName "陈叔叔工具箱.exe"
#define MyOutputName "AngkleChenToolBox-1.2.0-Full-Win-x86_64-Setup"

[Setup]
AppId={{A1B2C3D4-5678-9ABC-DEF0-1234567890AB}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppCopyright=Copyright (C) 2024 {#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir={#SourcePath}\..\发布
OutputBaseFilename={#MyOutputName}
SetupIconFile={#SourcePath}\..\logo.ico
Compression=lzma2/fast
SolidCompression=no
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
WizardImageAlphaFormat=defined
BackColor=$1F1F1F
BackColor2=$2D2D2D
; 中文界面
LanguageDetectionMethod=uilanguage
ShowLanguageDialog=no

[Languages]
Name: "zhcn"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加选项:"; Flags: checkedonce
Name: "startmenu"; Description: "创建开始菜单快捷方式"; GroupDescription: "附加选项:"; Flags: checkedonce

[Files]
Source: "{#SourcePath}\..\陈叔叔工具箱.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\..\工具\*"; DestDir: "{app}\工具"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourcePath}\..\扩展工具\*"; DestDir: "{app}\扩展工具"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startmenu
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCU; Subkey: "Software\{#MyAppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\{#MyAppName}"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"; Flags: uninsdeletekey
