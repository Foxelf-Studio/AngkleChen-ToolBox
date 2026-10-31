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
using 陈叔叔工具箱.Services;
using CatInfo = 陈叔叔工具箱.Models.CatInfo;

using DoubleAnimation = System.Windows.Media.Animation.DoubleAnimation;
using PowerEase = System.Windows.Media.Animation.PowerEase;
using EasingMode = System.Windows.Media.Animation.EasingMode;
using Storyboard = System.Windows.Media.Animation.Storyboard;
using TranslateTransform = System.Windows.Media.TranslateTransform;

namespace 陈叔叔工具箱;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    // ── 工具箱根目录（统一由 AppPaths 提供）──────────
    private static readonly string ToolboxRoot = AppPaths.Root;

    // ── 配置管理器 ────────────────────────────────
    private AppConfig _config = null!;


    // Segoe MDL2 Assets — Windows 内置官方图标字体
    // 分类同样来自 config.json，运行时由 LoadFromConfig() 填充
    private CatInfo[] Categories = [];

    // ── 工具数据 ──────────────────────────────────
    // 全部来自 config.json（唯一数据源），此字段只作运行时缓存，代码里不再维护第二份清单
    private ToolInfo[] AllTools = [];

    // ── 属性 ──────────────────────────────────────
    private string _headerTitle = "全部工具";
    public string HeaderTitle { get => _headerTitle; set { _headerTitle = value; OnPropChanged(); } }

    private string _headerSubtitle = "浏览所有便携工具";
    public string HeaderSubtitle { get => _headerSubtitle; set { _headerSubtitle = value; OnPropChanged(); } }

    private string _breadcrumb = "陈叔叔工具箱  ›  全部工具";
    public string Breadcrumb { get => _breadcrumb; set { _breadcrumb = value; OnPropChanged(); } }

    private string _statusText = "正在加载…";
    public string StatusText { get => _statusText; set { _statusText = value; OnPropChanged(); } }

    private string _statusTip = "";
    public string StatusTip { get => _statusTip; set { _statusTip = value; OnPropChanged(); } }

    // ── 顶部警告条（配置缺失/损坏这类"必须看见"的问题）──
    private string _warningText = "";
    public string WarningText { get => _warningText; set { _warningText = value; OnPropChanged(); } }

    private Visibility _warningVisible = Visibility.Collapsed;
    public Visibility WarningVisible { get => _warningVisible; set { _warningVisible = value; OnPropChanged(); } }

    /// <summary>在搜索框下方显示红色警告条</summary>
    private void ShowWarning(string text)
    {
        WarningText = text;
        WarningVisible = Visibility.Visible;
    }

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

    // ── 命令（只创建一次；此前是表达式体属性，每次绑定都 new 一个） ──
    public ICommand SelectCommand { get; }
    public ICommand LaunchCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    // ── 设置页面 ──────────────────────────────────
    private bool _isSettingsOpen;

    // ── 构造函数 ──────────────────────────────────
    public MainWindow()
    {
        InitializeComponent();

        SelectCommand = new RelayCommand(OnSelectTool);
        LaunchCommand = new RelayCommand(OnLaunchTool);
        EditCommand = new RelayCommand(OnEditTool);
        DeleteCommand = new RelayCommand(OnDeleteTool);

        DataContext = this;

        // 初始化图标缓存
        IconHelper.Init(ToolboxRoot);

        // 加载配置
        _config = AppConfig.Load();

        _suppressAnim = true;

        // 从配置加载分类和工具
        LoadFromConfig();

        // 初始化指示条
        Dispatcher.BeginInvoke(new Action(() => MoveIndicator(0, false)),
            System.Windows.Threading.DispatcherPriority.Loaded);

        // 延迟加载图标和检查更新
        Loaded += (_, _) =>
        {
            // 配置缺失/损坏时如实告知，而不是让用户以为工具"自己变少了"
            if (_config.RestoredFromTemplate)
            {
                ShowWarning(_config.BrokenBackupFile != null
                    ? $"config.json 无法解析，原文件已备份为程序目录下的 {_config.BrokenBackupFile}，当前显示的是内置清单。备份里保留着你自己加过的工具，可以手工取回。"
                    : "config.json 缺失，已用内置清单重建。之前自己添加过的工具需要重新加一次。");
            }

            // 后台刷新图标（源文件被替换时会自动重新提取）
            _ = RefreshIconsAsync();

            // 预热"文件是否存在"快照，之后搜索不再碰磁盘
            _ = InvalidateExistsCacheAsync();

            // 启动维护：先找回被移动的工具，再发现未收录的条目
            _ = StartupMaintenanceAsync();

            // 诊断：开启日志时记录虚拟化实际实例化的卡片数
            Dispatcher.BeginInvoke(new Action(() =>
                Logger.Log($"卡片虚拟化：已实例化 {VirtualizingWrapPanel.LastRealizedCount} / {AllTools.Length}")),
                System.Windows.Threading.DispatcherPriority.Background);

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

    // ── 从配置加载数据 ────────────────────────────
    /// <param name="selectCategoryId">刷新后保持选中的分类 id；为空则回到"全部工具"</param>
    private void LoadFromConfig(string? selectCategoryId = null)
    {
        // 先加载工具（必须在 SelectedIndex 之前，否则触发 OnCategoryChanged 时 AllTools 还是旧的）
        AllTools = _config.GetAllTools().ToArray();

        // 再加载分类
        var categories = new List<CatInfo>
        {
            new("all", "全部工具", "", "浏览所有工具")
        };
        categories.AddRange(_config.GetCategories());
        Categories = categories.ToArray();
        CategoryList.ItemsSource = Categories;

        var index = 0;
        if (!string.IsNullOrEmpty(selectCategoryId))
        {
            var target = Array.FindIndex(Categories, c => c.Id == selectCategoryId);
            if (target >= 0) index = target;
        }

        CategoryList.SelectedIndex = index;
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
    private static readonly string TrashIcon =
        "M 3 3 L 5 3 L 5 1 L 11 1 L 11 3 L 13 3 L 13 5 L 3 5 Z M 4 5 L 12 5 L 11 15 L 5 15 Z";

    private static readonly string PencilIcon =
        "M 3 17.25 V 21 h 3.75 L 17.81 9.94 l -3.75 -3.75 L 3 17.25 Z M 20.71 7.04 c 0.39 -0.39 0.39 -1.02 0 -1.41 l -2.34 -2.34 c -0.39 -0.39 -1.02 -0.39 -1.41 0 l -1.83 1.83 3.75 3.75 1.83 -1.83 Z";

    private void NavItem_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBoxItem item) return;
        if (item.DataContext is not CatInfo cat) return;
        if (cat.Id == "all") return; // 不允许删除"全部工具"

        e.Handled = true;
        Win11ContextMenu.Show(PointToScreen(e.GetPosition(this)),
            new MenuAction("重命名分类", PencilIcon, () => RenameCategory(cat)),
            MenuAction.Separator,
            new MenuAction("删除分类", TrashIcon, () => DeleteCategory(cat), IsDestructive: true));
    }

    /// <summary>
    /// 重命名分类：目录跟着改名，分类下所有工具路径同步改写。
    /// 目录改不动就整体中止，配置不会半改不改。
    /// </summary>
    private void RenameCategory(CatInfo cat)
    {
        var dialog = new InputDialog(
            "重命名分类",
            "分类名称",
            cat.Name,
            $"改完后 工具\\{cat.Name} 文件夹也会一起改名，该分类下的工具路径会自动更新。")
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true) return;
        if (dialog.Value == cat.Name) return;

        if (!_config.RenameCategory(cat.Id, dialog.Value))
        {
            CustomMessageBox.Show(
                "重命名失败。常见原因：\n\n" +
                "· 已存在同名分类\n" +
                "· 目标文件夹已存在\n" +
                "· 文件夹里的程序正在运行，导致目录无法改名",
                "重命名失败");
            return;
        }

        LoadFromConfig(cat.Id);
        _ = InvalidateExistsCacheAsync();
        ApplyFilter();
        StatusTip = $"分类已重命名为「{dialog.Value}」";
    }

    private void DeleteCategory(CatInfo cat)
    {
        Logger.Log($"=== 开始删除分类: {cat.Name} ===");

        var categoryDir = Path.Combine(ToolboxRoot, "工具", cat.Name);
        bool hasTools = Directory.Exists(categoryDir) && Directory.GetFiles(categoryDir, "*", SearchOption.AllDirectories).Length > 0;
        Logger.Log($"分类目录: {categoryDir}, 是否有工具: {hasTools}");

        string message;
        if (hasTools)
        {
            message = $"分类 \"{cat.Name}\" 包含工具文件。\n\n" +
                      "该分类的文件夹和工具条目会一起移入工具箱回收站，可在「设置 → 回收站」中恢复。\n\n确定要继续吗？";
        }
        else
        {
            message = $"确定要删除空分类 \"{cat.Name}\" 吗？\n\n（会移入回收站，可恢复）";
        }

        var result = CustomMessageBox.Show(message, "确认删除", MessageBoxButton.YesNo);
        if (result != MessageBoxResult.Yes)
        {
            Logger.Log("用户取消删除分类");
            return;
        }

        try
        {
            if (!_config.MoveCategoryToTrash(cat.Id))
            {
                Logger.Log("分类移入回收站失败");
                CustomMessageBox.Show(
                    $"无法删除分类「{cat.Name}」。\n\n有文件正在被占用，请关闭相关程序后重试。",
                    "删除失败");
                return;
            }

            LoadFromConfig();
            Logger.Log($"重新加载完成, 工具总数: {AllTools.Length}");

            if (_activeCategory == cat.Id)
                CategoryList.SelectedIndex = 0;

            ApplyFilter();
            StatusTip = $"已删除分类 {cat.Name}，已移入回收站";
            Logger.Log($"=== 删除分类完成: {cat.Name} ===");
        }
        catch (Exception ex)
        {
            Logger.Log($"删除分类失败: {ex.Message}");
            Logger.Log($"异常堆栈: {ex.StackTrace}");
            CustomMessageBox.Show($"删除失败: {ex.Message}", "错误");
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
        var existingCategories = _config.Categories.Select(c => c.Name).ToArray();
        var dialog = new AddToolDialog(ToolboxRoot, existingCategories) { Owner = this };

        if (dialog.ShowDialog() != true) return;

        var result = ToolImporter.Import(
            _config,
            ToolboxRoot,
            dialog.SourcePath!,
            dialog.CategoryName!,
            dialog.ToolName!,
            dialog.ToolDescription ?? "",
            dialog.CategoryIcon);

        if (!result.Success)
        {
            StatusTip = result.Message;
            CustomMessageBox.Show(result.Message, "提示");
            return;
        }

        LoadFromConfig();
        _ = InvalidateExistsCacheAsync();
        ApplyFilter();
        StatusTip = result.Message;
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
        Logger.Log($"OnCategoryChanged: 触发, SelectedItem={CategoryList.SelectedItem?.ToString()}");
        if (CategoryList.SelectedItem is CatInfo cat)
        {
            var cats = Categories;
            var newIndex = Array.FindIndex(cats, c => c.Id == cat.Id);
            var oldIndex = _prevCatIndex;
            _prevCatIndex = newIndex;

            _activeCategory = cat.Id;
            Logger.Log($"OnCategoryChanged: cat={cat.Name}, newIndex={newIndex}, oldIndex={oldIndex}, _suppressAnim={_suppressAnim}");

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

        // 绑定同一份配置实例，并刷新回收站数量
        SettingsPanelCtrl.Bind(_config);

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

    /// <summary>设置面板里恢复了工具 / 清空了回收站，刷新主界面</summary>
    private void OnSettingsToolsChanged(object? sender, EventArgs e)
    {
        LoadFromConfig();
        ApplyFilter();
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
                ShowUpdateToast(result.LatestVersion);
            }
        }
        catch
        {
            // 静默失败
        }
    }

    private void ShowUpdateToast(string latestVersion)
    {
        TxtToastMessage.Text = $"发现 v{latestVersion}，点击查看更新";
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

    // ── 搜索（带防抖，避免每个按键都重排一遍列表） ──
    private System.Windows.Threading.DispatcherTimer? _searchDebounce;

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (_searchDebounce == null)
        {
            _searchDebounce = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(120)
            };
            _searchDebounce.Tick += (_, _) =>
            {
                _searchDebounce!.Stop();
                ApplyFilter();
            };
        }

        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    // ── 文件存在性快照：避免每次过滤都做 N 次同步 IO ──
    private volatile Dictionary<string, bool>? _existsSnapshot;

    private bool ToolExists(ToolInfo tool)
    {
        var snapshot = _existsSnapshot;
        if (snapshot != null && snapshot.TryGetValue(tool.RelativePath, out var cached))
            return cached;

        // 快照还没建好时退回实时查询
        var path = Path.Combine(ToolboxRoot, tool.RelativePath);
        return File.Exists(path) || Directory.Exists(path);
    }

    /// <summary>工具增删后重建存在性快照（后台线程算，主线程只换引用）</summary>
    private async Task InvalidateExistsCacheAsync()
    {
        var tools = AllTools;
        var root = ToolboxRoot;

        var snapshot = await Task.Run(() =>
        {
            var map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var tool in tools)
            {
                var path = Path.Combine(root, tool.RelativePath);
                map[tool.RelativePath] = File.Exists(path) || Directory.Exists(path);
            }
            return map;
        });

        _existsSnapshot = snapshot;
        ApplyFilter();   // 快照换新后刷一次，保证界面与磁盘实际状态一致
    }

    /// <summary>
    /// 启动时后台刷新图标；有更新就丢掉内存缓存并重刷列表，避免显示旧图标
    /// </summary>
    private async Task RefreshIconsAsync()
    {
        var refreshed = await Task.Run(() => _config.CacheAllIcons());
        if (refreshed <= 0) return;

        IconHelper.ClearCache();
        ApplyFilter();
        Logger.Log($"图标刷新完成: {refreshed} 个，界面已重载");
    }

    private void ApplyFilter()
    {
        var query = SearchBox.Text.Trim();
        var filtered = AllTools.Where(ToolExists);

        if (_activeCategory != "all")
            filtered = filtered.Where(t => t.CategoryId == _activeCategory);

        if (!string.IsNullOrEmpty(query))
        {
            filtered = filtered.Where(t =>
                t.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Category.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        // 按导航栏顺序排序（先建索引字典，避免 OrderBy 里做 O(n*m) 查找）
        var categoryOrder = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < Categories.Length; i++)
            categoryOrder[Categories[i].Id] = i;

        var list = filtered
            .OrderBy(t => categoryOrder.TryGetValue(t.CategoryId, out var index) ? index : 999)
            .ThenBy(t => t.Order)                                   // 手动拖拽调整过的顺序优先
            .ThenBy(t => t.Name, StringComparer.CurrentCulture)
            .ToList();

        CardItems.ItemsSource = list;
        StatusText = $"共 {list.Count} 款工具  ·  v{VersionInfo.Current}";
    }

    // ── 工具选中（单击） ─────────────────────────
    private void OnSelectTool(object? parameter)
    {
        if (parameter is not ToolInfo tool) return;

        _selectedTool = tool;
        SelectedToolName = tool.Name;
        SelectedToolDescription = string.IsNullOrEmpty(tool.Detail) ? tool.Description : tool.Detail;
        SelectedToolCategory = tool.Category;
        SelectedToolIcon = IconHelper.GetIcon(tool.RelativePath);
        DescDetailVisible = "Visible";
        DescPlaceholderVisible = "Collapsed";
    }

    // ── 工具启动（双击） ──────────────────────────
    private void OnLaunchTool(object? parameter)
    {
        if (parameter is not ToolInfo tool) return;

        var error = ToolLauncher.Launch(ToolboxRoot, tool);
        if (error != null)
        {
            CustomMessageBox.Show(error, "提示");
            return;
        }

        StatusTip = $"已启动 {tool.Name}";
    }

    // ── 编辑工具（改名 / 改描述） ─────────────────
    private void OnEditTool(object? parameter)
    {
        if (parameter is not ToolInfo tool) return;

        var dialog = new EditToolDialog(tool) { Owner = this };
        if (dialog.ShowDialog() != true) return;

        if (!_config.UpdateTool(tool.Id, dialog.ToolName, dialog.ToolDescription))
        {
            CustomMessageBox.Show("保存失败：配置里找不到这个工具，或配置文件不可写。", "提示");
            return;
        }

        LoadFromConfig();
        ApplyFilter();
        StatusTip = $"已更新「{dialog.ToolName}」";
    }

    // ── 拖拽排序 ──────────────────────────────────
    /// <summary>只在"单个分类 + 未搜索"时允许拖拽，混合视图里拖动的语义不明确</summary>
    private bool CanReorder => _activeCategory != "all" && string.IsNullOrWhiteSpace(SearchBox.Text);

    private void OnCardDragOver(object sender, DragEventArgs e)
    {
        e.Effects = CanReorder && e.Data.GetDataPresent(typeof(ToolInfo))
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnCardDrop(object sender, DragEventArgs e)
    {
        if (!CanReorder) return;
        if (e.Data.GetData(typeof(ToolInfo)) is not ToolInfo dragged) return;

        var point = e.GetPosition(CardItems);
        var targetCard = HitTestCard(point);
        if (targetCard?.DataContext is not ToolInfo target) return;
        if (target.RelativePath.Equals(dragged.RelativePath, StringComparison.OrdinalIgnoreCase)) return;

        // 落在卡片上半部 → 插到它前面；下半部 → 插到它后面
        var cardTop = targetCard.TranslatePoint(new Point(0, 0), CardItems).Y;
        var insertAfter = point.Y > cardTop + targetCard.ActualHeight / 2;

        if (!_config.ReorderTool(_activeCategory, dragged.Id, target.Id, insertAfter))
            return;

        LoadFromConfig();
        ApplyFilter();
        StatusTip = $"已把「{dragged.Name}」移到新位置";
    }

    /// <summary>找出鼠标位置下的卡片</summary>
    private ToolCard? HitTestCard(Point point)
    {
        ToolCard? found = null;

        VisualTreeHelper.HitTest(CardItems, null, result =>
        {
            DependencyObject? node = result.VisualHit;
            while (node != null && node is not ToolCard)
            {
                node = node is Visual
                    ? VisualTreeHelper.GetParent(node)
                    : LogicalTreeHelper.GetParent(node);
            }

            if (node is ToolCard card)
            {
                found = card;
                return HitTestResultBehavior.Stop;
            }

            return HitTestResultBehavior.Continue;
        }, new PointHitTestParameters(point));

        return found;
    }

    // ── 扫描未收录的工具 ──────────────────────────
    /// <summary>
    /// 启动维护：先找回被移动的工具，再发现未收录的条目。
    /// 顺序不能反——路径没修的话，被搬走的工具会被当成"新工具"再收一遍，于是同一个程序出现两条记录。
    /// </summary>
    private async Task StartupMaintenanceAsync()
    {
        await RepairMissingPathsAsync();
        await ScanUnregisteredAsync();
    }

    /// <summary>
    /// 路径失效时在 工具\ 下按「文件名 + 体积」把工具找回来。
    /// 只有唯一命中才改配置；候选不唯一一律跳过并记日志。
    /// </summary>
    private async Task RepairMissingPathsAsync()
    {
        RelinkResult result;

        try
        {
            result = await Task.Run(() => ToolRelinker.FindRelinks(_config, ToolboxRoot));
        }
        catch (Exception ex)
        {
            Logger.Log($"重新定位失败: {ex.Message}");
            return;
        }

        if (result.Ambiguous.Count > 0)
        {
            Logger.Log($"重新定位跳过 {result.Ambiguous.Count} 个候选不唯一的工具: " +
                       string.Join("；", result.Ambiguous));
        }

        if (result.Hits.Count == 0)
        {
            Logger.Log("重新定位: 没有失效路径");
            return;
        }

        var fixedCount = 0;
        var failed = 0;
        foreach (var hit in result.Hits)
        {
            if (_config.RelinkTool(hit.ToolId, hit.NewRelativePath)) fixedCount++;
            else failed++;
        }

        var byNameOnly = result.Hits.Count(h => !h.MatchedBySize);
        Logger.Log($"重新定位: 命中 {result.Hits.Count}，成功 {fixedCount}，失败 {failed}" +
                   (byNameOnly > 0 ? $"，其中按名字匹配（体积未知）{byNameOnly} 个" : ""));

        if (fixedCount == 0) return;

        LoadFromConfig(_activeCategory);
        _ = InvalidateExistsCacheAsync();
        ApplyFilter();

        StatusTip = $"已找回 {fixedCount} 个被移动过位置的工具";
    }

    /// <summary>
    /// 启动时扫一遍 工具\，发现配置里没有的条目就问一句要不要收录。只提示，不自动改配置。
    /// </summary>
    private async Task ScanUnregisteredAsync()
    {
        List<ScanCandidate> found;

        try
        {
            found = await Task.Run(() => ToolScanner.ScanUnregistered(_config, ToolboxRoot));
            Logger.Log($"扫描未收录条目: {found.Count} 个");
        }
        catch (Exception ex)
        {
            Logger.Log($"扫描未收录条目失败: {ex.Message}");
            return;
        }

        if (found.Count == 0) return;

        var preview = string.Join("\n", found.Take(8).Select(f => $"· {f.CategoryName} / {f.ToolName}"));
        if (found.Count > 8) preview += $"\n· 另有 {found.Count - 8} 个……";

        var answer = CustomMessageBox.Show(
            $"在 工具\\ 目录里发现 {found.Count} 个未收录的条目：\n\n{preview}\n\n" +
            "要按目录名把它们加进工具箱吗？描述会写成「自动收录」，之后可右键编辑。",
            "发现未收录的工具",
            MessageBoxButton.YesNo);

        if (answer != MessageBoxResult.Yes) return;

        var added = 0;
        foreach (var candidate in found)
        {
            var ok = _config.AddTool(candidate.CategoryName, new AppConfig.ToolConfig
            {
                Name = candidate.ToolName,
                Description = "自动收录，可右键编辑名称与描述。",
                RelativePath = candidate.RelativePath
            });

            if (!ok) continue;

            added++;
            AppConfig.CacheIcon(candidate.RelativePath);
        }

        if (added == 0) return;

        LoadFromConfig();
        _ = InvalidateExistsCacheAsync();
        ApplyFilter();
        StatusTip = $"已收录 {added} 个工具";
    }

    // ── 删除工具（移入回收站，可恢复） ─────────────
    private void OnDeleteTool(object? parameter)
    {
        if (parameter is not ToolInfo t) return;

        Logger.Log($"=== 开始删除工具: {t.Name} ===");

        try
        {
            // 先搬文件、再改配置；任一环节失败整体回滚，配置保持原样
            if (!_config.MoveToolToTrash(t.Id))
            {
                CustomMessageBox.Show(
                    $"无法删除「{t.Name}」。\n\n文件可能正在运行，请先关闭该工具后重试。",
                    "删除失败");
                return;
            }

            LoadFromConfig();
            _ = InvalidateExistsCacheAsync();
            ApplyFilter();

            _lastDeletedId = _config.Trash.FirstOrDefault()?.Id;
            ShowUndoBar(t.Name);

            Logger.Log($"=== 删除工具完成: {t.Name} ===");
        }
        catch (Exception ex)
        {
            Logger.Log($"删除工具失败: {ex.Message}");
            CustomMessageBox.Show($"删除失败: {ex.Message}", "错误");
        }
    }

    private string? _lastDeletedId;
    private System.Windows.Threading.DispatcherTimer? _undoTimer;

    /// <summary>
    /// 删除后给出 15 秒撤销窗口，不依赖用户自己去找回收站
    /// </summary>
    private void ShowUndoBar(string name)
    {
        _undoTimer?.Stop();
        StatusTip = $"已删除 {name}，文件已移入回收站";
        BtnUndoDelete.Visibility = Visibility.Visible;

        _undoTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _undoTimer.Tick += (_, _) =>
        {
            _undoTimer?.Stop();
            BtnUndoDelete.Visibility = Visibility.Collapsed;
        };
        _undoTimer.Start();
    }

    private void OnUndoDelete(object sender, RoutedEventArgs e)
    {
        _undoTimer?.Stop();
        BtnUndoDelete.Visibility = Visibility.Collapsed;

        if (_lastDeletedId == null || !_config.RestoreFromTrash(_lastDeletedId))
        {
            CustomMessageBox.Show("恢复失败，可稍后在「设置 → 回收站」中重试。", "提示");
            return;
        }

        _lastDeletedId = null;
        LoadFromConfig();
        _ = InvalidateExistsCacheAsync();
        ApplyFilter();
        StatusTip = "已撤销删除";
    }

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
