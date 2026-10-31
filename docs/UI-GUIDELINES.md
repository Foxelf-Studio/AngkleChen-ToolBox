# 陈叔叔工具箱 项目配置

## 窗口样式规范

### 弹窗规范
- 所有弹窗必须使用 Windows 原生窗口（`WindowStyle="SingleBorderWindow"`）
- 标题栏颜色必须设置为深色（#181818），与主程序标题栏一致
- 使用 `WindowChrome` 设置标题栏高度和圆角

### 按钮动画规范
- 所有按钮必须有线性高亮动画
- 参考工具卡片（ToolCard）的动画实现：
  - 背景色：#2d2d2d → #353535（hover时）
  - 边框色：#33ffffff → #2fffffff（hover时）
  - 动画时长：0.15秒
  - 使用 `ColorAnimation` 实现平滑过渡

### 颜色规范
- 主背景：#1f1f1f
- 侧边栏/标题栏：#181818
- 卡片/弹窗背景：#2d2d2d
- 卡片hover：#353535
- 强调色：#60cdff（蓝色）
- 按钮hover：#4aa8d8 → #5cb8e8
- 主文字：#ffffff
- 次要文字：#cccccc
- 提示文字：#777777
- 警告色（仅用于必须被看见的问题）：底 #29ff5a5f / 边 #5cff5a5f / 图标 #ff7a7e / 文字 #ffc2c4

### 输入框规范
- **必须显式设置光标颜色**：WPF 的 `CaretBrush` 默认是纯黑，在暗色输入框里几乎看不见。
  全项目由 `App.xaml` 里的隐式 `TargetType="TextBox"` 样式统一兜底（`CaretBrush=#ffffff`），
  新增输入框不要再单独写一遍；如果给 TextBox 指定了自定义 `Style`，记得那个样式里也要带上 `CaretBrush`。
- 输入框容器：`Background=#1effffff`、圆角 4、边框 `#3effffff`、内边距 `8,6`
- 选中高亮：`SelectionBrush=#5A60CDFF`

### 警告提示规范
- **必须被看见的问题（配置损坏、数据可能丢失）放在搜索框下方**，用红底圆角条，不要塞进状态栏小字
- 形状与搜索框一致：圆角 8，左右铺满内容区
- 结构：图标（Segoe MDL2 `&#xE7BA;`）+ 说明文字（可换行）+ 可选操作
- 状态栏只放"共 N 款工具 / 临时操作反馈"这类可以一扫而过的东西

### 对话框实现示例
```xml
<Window ...
        WindowStyle="SingleBorderWindow"
        Background="#2d2d2d">
    <WindowChrome.WindowChrome>
        <WindowChrome GlassFrameThickness="-1"
                      CornerRadius="0"
                      CaptionHeight="32"
                      UseAeroCaptionButtons="True" />
    </WindowChrome.WindowChrome>
    <!-- 内容 -->
</Window>
```

### 按钮样式示例
```xml
<Button Content="确定"
        Background="#4aa8d8"
        BorderBrush="#5cb8e8" BorderThickness="1"
        Style="{StaticResource AccentButton}" />
```
