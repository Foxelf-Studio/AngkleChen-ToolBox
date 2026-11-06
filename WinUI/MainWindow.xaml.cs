using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using 陈叔叔工具箱.Helpers;
using 陈叔叔工具箱.Models;
using 陈叔叔工具箱.Services;

namespace AngkleChen;

public sealed partial class MainWindow : Window
{
    private readonly AppConfig _config;
    private readonly AppSettings _settings;
    private string _activeCategoryId = "all";
    private ToolCardVm? _selected;
    private int _refreshGeneration;
    private bool _syncingSettings;

    public ObservableCollection<ToolCardVm> Tools { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        Title = "陈叔叔工具箱";

        AppWindow.Resize(new Windows.Graphics.SizeInt32(1180, 760));

        // Mica 只在 Win11 可用，Win10 上保持纯色背景（README 声明支持 1809+）
        if (MicaController.IsSupported())
            SystemBackdrop = new MicaBackdrop();

        _config = AppConfig.Load();
        _settings = AppSettings.Load();
        Logger.IsEnabled = _settings.EnableLog;

        ShowConfigWarning();

        // 系统标题栏跟随应用主题：不显式设置的话深色界面会配一条白标题栏
        RootGrid.ActualThemeChanged += (_, _) =>
            TitleBarTheme.Apply(this, RootGrid.ActualTheme == ElementTheme.Dark);
        DispatcherQueue.TryEnqueue(() =>
            TitleBarTheme.Apply(this, RootGrid.ActualTheme == ElementTheme.Dark));

        BuildNavigation();
        _ = InitializeAsync();
    }

    /// <summary>
    /// 启动维护：先把被搬走的工具找回来，再刷列表。
    /// 顺序不能反——先刷的话被搬走的工具会短暂消失一次，观感像丢数据。
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            var result = await Task.Run(() => ToolRelinker.FindRelinks(_config, AppPaths.Root));

            var fixedCount = 0;
            foreach (var hit in result.Hits)
                if (_config.RelinkTool(hit.ToolId, hit.NewRelativePath)) fixedCount++;

            if (fixedCount > 0)
                StatusTip.Text = $"已找回 {fixedCount} 个被移动过位置的工具";
        }
        catch
        {
            // 自愈失败不影响正常浏览，静默放过
        }

        RefreshList();
        // 首次启动（配置迁移拖慢首帧）时，导航可能在菜单项就绪前就触发了
        // SelectionChanged，把选中态落到别的分类上。这里在布局完成后兜一次底。
        DispatcherQueue.TryEnqueue(() =>
        {
            if (Nav.MenuItems.Count > 0 && !ReferenceEquals(Nav.SelectedItem, Nav.MenuItems[0]))
                Nav.SelectedItem = Nav.MenuItems[0];
        });
    }

    /// <summary>配置缺失/损坏时在搜索框下方挂红条，说清后果与出路</summary>
    private void ShowConfigWarning()
    {
        if (!_config.RestoredFromTemplate) return;

        WarningText.Text = _config.BrokenBackupFile != null
            ? $"config.json 无法解析，原文件已备份为程序目录下的 {_config.BrokenBackupFile}，当前显示的是内置清单。备份里保留着你自己加过的工具，可以手工取回。"
            : "config.json 缺失，已用内置清单重建。之前自己添加过的工具需要重新加一次。";

        WarningBar.Visibility = Visibility.Visible;
    }

    private void BuildNavigation()
    {
        Nav.MenuItems.Clear();

        var all = new NavigationViewItem
        {
            Content = "全部工具",
            Tag = "all",
            Icon = new FontIcon { Glyph = "\uE71D" }
        };
        Nav.MenuItems.Add(all);

        foreach (var cat in _config.Categories)
        {
            Nav.MenuItems.Add(new NavigationViewItem
            {
                Content = cat.Name,
                Tag = cat.Id,
                Icon = new FontIcon
                {
                    Glyph = string.IsNullOrEmpty(cat.Icon) ? "\uE8B7" : cat.Icon
                }
            });
        }

        Nav.SelectedItem = all;
    }

    private void ShowCategory(string categoryId)
    {
        ShowTools();
        _activeCategoryId = categoryId;
        RefreshList();

    }

    private async void RefreshList()
    {
        var generation = ++_refreshGeneration;
        var query = SearchBox?.Text?.Trim() ?? "";

        var all = _config.GetAllTools();
        var root = AppPaths.Root;

        // 存在性检查放后台：90 次文件系统查询不该压在主线程上
        var snapshot = await Task.Run(() => all
            .Select(t => new
            {
                Tool = t,
                Exists = File.Exists(Path.Combine(root, t.RelativePath))
                         || Directory.Exists(Path.Combine(root, t.RelativePath))
            })
            .ToList());

        // 输入过快时会有多次请求在飞，只认最后一次
        if (generation != _refreshGeneration) return;

        var source = snapshot.Where(x => x.Exists).Select(x => x.Tool);

        if (_activeCategoryId != "all")
            source = source.Where(t => t.CategoryId == _activeCategoryId);

        if (query.Length > 0)
        {
            source = source.Where(t =>
                t.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Category.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        var list = source.ToList();

        var keepSelectedId = _selected?.Id;

        Tools.Clear();
        foreach (var tool in list)
        {
            var vm = new ToolCardVm(tool);
            Tools.Add(vm);
            _ = LoadIconAsync(vm);
        }

        ToolGrid.ItemsSource = Tools;
        EmptyHint.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        var again = keepSelectedId == null ? null : Tools.FirstOrDefault(t => t.Id == keepSelectedId);
        if (again != null) Select(again);
        else ClearSelection();

        var active = _activeCategoryId == "all"
            ? null
            : _config.Categories.FirstOrDefault(c => c.Id == _activeCategoryId);

        HeaderTitle.Text = active?.Name ?? "全部工具";
        HeaderSubtitle.Text = query.Length > 0
            ? $"搜索「{query}」的结果"
            : $"共 {list.Count} 款工具";

        StatusText.Text = $"共 {list.Count} 款工具 · v{VersionInfo.Current}";
    }

    private async Task LoadIconAsync(ToolCardVm vm)
    {
        var bytes = await IconProvider.LoadBytesAsync(vm.RelativePath).ConfigureAwait(false);
        if (bytes == null) return;

        // BitmapImage 必须在 UI 线程建，交给 DispatcherQueue 处理
        DispatcherQueue.TryEnqueue(async () =>
        {
            var icon = await IconProvider.ToBitmapAsync(bytes);
            if (icon == null) return;

            vm.Icon = icon;

            if (ReferenceEquals(vm, _selected))
                DetailIcon.Source = icon;
        });
    }

    private void Select(ToolCardVm vm)
    {
        foreach (var other in Tools)
            other.IsSelected = ReferenceEquals(other, vm);

        _selected = vm;

        DetailName.Text = vm.Name;
        DetailCategory.Text = vm.Category;
        DetailDescription.Text = string.IsNullOrWhiteSpace(vm.Description)
            ? "（没有描述）"
            : vm.Description;
        DetailIcon.Source = vm.Icon;

        DetailPanel.Visibility = Visibility.Visible;
        DetailPlaceholder.Visibility = Visibility.Collapsed;
    }

    private void ClearSelection()
    {
        _selected = null;
        DetailPanel.Visibility = Visibility.Collapsed;
        DetailPlaceholder.Visibility = Visibility.Visible;
    }

    private void OnCategoryChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ShowSettings();
            return;
        }

        if (args.SelectedItem is NavigationViewItem { Tag: string id })
            ShowCategory(id);
    }

    /// <summary>
    /// 齿轮走 NavigationView 的 SettingsItem（左侧导航最底部），
    /// 点击后内容区整体切成设置页，而不是像原来那样在标题栏挂个按钮弹面板。
    /// </summary>
    private void ShowSettings()
    {
        ToolsView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Visible;
        StatusTip.Text = "";

        SyncSettingsUi();
        RefreshTrash();
    }

    // ── 设置页 ──────────────────────────────────────
    private void SyncSettingsUi()
    {
        _syncingSettings = true;
        ToggleAutoCheck.IsOn = _settings.AutoCheckUpdate;
        ToggleLog.IsOn = _settings.EnableLog;
        _syncingSettings = false;
    }

    private void OnSettingsToggled(object sender, RoutedEventArgs e)
    {
        // 同步控件状态时也会触发 Toggled，别把它当成用户操作写盘
        if (_syncingSettings) return;

        _settings.AutoCheckUpdate = ToggleAutoCheck.IsOn;
        _settings.EnableLog = ToggleLog.IsOn;
        _settings.Save();

        Logger.IsEnabled = _settings.EnableLog;
        StatusTip.Text = "设置已保存";
    }

    private void RefreshTrash()
    {
        var entries = _config.Trash
            .Select(t => new TrashItemVm(t.Id, t.Name, $"{t.Category} · {t.RelativePath}"))
            .ToList();

        TrashList.ItemsSource = entries;
        TrashEmptyHint.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        BtnEmptyTrash.IsEnabled = entries.Count > 0;
    }

    private void OnRestoreTrash(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string id }) return;
        if (!_config.RestoreFromTrash(id)) return;

        RefreshTrash();
        BuildNavigation();
        RefreshList();
        StatusTip.Text = "已恢复";
    }

    private async void OnEmptyTrash(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmAsync("清空回收站", "回收站里的文件和记录会被永久删除，无法恢复。")) return;

        _config.EmptyTrash();
        RefreshTrash();
        StatusTip.Text = "回收站已清空";
    }

    private async Task<bool> ConfirmAsync(string title, string message, string primary = "确定")
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = primary,
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = RootGrid.XamlRoot
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private void ShowTools()
    {
        SettingsView.Visibility = Visibility.Collapsed;
        ToolsView.Visibility = Visibility.Visible;
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e) => RefreshList();

    private void OnToolClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ToolCardVm vm)
            Select(vm);
    }

    private void OnCardDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ToolCardVm vm })
            Launch(vm);
    }

    private ToolInfo? FindTool(ToolCardVm vm) =>
        _config.GetAllTools().FirstOrDefault(t => t.Id == vm.Id);

    private void Launch(ToolCardVm vm)
    {
        var tool = FindTool(vm);
        if (tool == null) return;

        var error = ToolLauncher.Launch(AppPaths.Root, tool);
        StatusTip.Text = error != null
            ? error.Replace('\n', ' ')
            : $"已启动 {tool.Name}";
    }

    // ── 右键菜单 ────────────────────────────────────
    private static ToolCardVm? VmOf(object sender) =>
        (sender as FrameworkElement)?.DataContext as ToolCardVm;

    private void OnMenuLaunch(object sender, RoutedEventArgs e)
    {
        if (VmOf(sender) is { } vm) Launch(vm);
    }

    private async void OnMenuEdit(object sender, RoutedEventArgs e)
    {
        if (VmOf(sender) is not { } vm) return;

        var nameBox = new TextBox { Text = vm.Name, MaxLength = 60 };
        var descBox = new TextBox
        {
            Text = vm.Description,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 96
        };

        var panel = new StackPanel { Spacing = 6 };
        panel.Children.Add(new TextBlock { Text = "名称", FontSize = 12 });
        panel.Children.Add(nameBox);
        panel.Children.Add(new TextBlock { Text = "描述", FontSize = 12, Margin = new Thickness(0, 8, 0, 0) });
        panel.Children.Add(descBox);

        var dialog = new ContentDialog
        {
            Title = "编辑工具",
            Content = panel,
            PrimaryButtonText = "保存",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = RootGrid.XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        var name = nameBox.Text.Trim();
        if (name.Length == 0)
        {
            StatusTip.Text = "名称不能为空";
            return;
        }

        if (_config.UpdateTool(vm.Id, name, descBox.Text.Trim()))
        {
            RefreshList();
            StatusTip.Text = $"已更新「{name}」";
        }
    }

    private async void OnMenuDelete(object sender, RoutedEventArgs e)
    {
        if (VmOf(sender) is not { } vm) return;
        if (!await ConfirmAsync("移入回收站", $"「{vm.Name}」的文件会被移到 回收站\\ 目录，可以再恢复。", "移入回收站")) return;

        if (_config.MoveToolToTrash(vm.Id))
        {
            RefreshList();
            BuildNavigation();
            StatusTip.Text = $"已移除 {vm.Name}";
        }
    }

    // ── 添加工具 ────────────────────────────────────
    private async void OnAddTool(object sender, RoutedEventArgs e)
    {
        var categoryBox = new ComboBox
        {
            ItemsSource = _config.Categories.Select(c => c.Name).ToList(),
            SelectedIndex = _config.Categories.Count > 0 ? 0 : -1,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var pathBox = new TextBox { IsReadOnly = true, PlaceholderText = "选择 exe 或整个工具文件夹" };
        var nameBox = new TextBox { PlaceholderText = "留空则用文件名" };
        var descBox = new TextBox
        {
            PlaceholderText = "一句话说明这个工具干什么",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 72
        };

        var browseFile = new Button { Content = "选文件" };
        var browseFolder = new Button { Content = "选文件夹" };

        var pathRow = new Grid { ColumnSpacing = 8 };
        pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pathBox, 0);
        Grid.SetColumn(browseFile, 1);
        Grid.SetColumn(browseFolder, 2);
        pathRow.Children.Add(pathBox);
        pathRow.Children.Add(browseFile);
        pathRow.Children.Add(browseFolder);

        var panel = new StackPanel { Spacing = 6 };
        panel.Children.Add(new TextBlock { Text = "分类", FontSize = 12 });
        panel.Children.Add(categoryBox);
        panel.Children.Add(new TextBlock { Text = "文件", FontSize = 12, Margin = new Thickness(0, 8, 0, 0) });
        panel.Children.Add(pathRow);
        panel.Children.Add(new TextBlock { Text = "名称", FontSize = 12, Margin = new Thickness(0, 8, 0, 0) });
        panel.Children.Add(nameBox);
        panel.Children.Add(new TextBlock { Text = "描述", FontSize = 12, Margin = new Thickness(0, 8, 0, 0) });
        panel.Children.Add(descBox);

        var dialog = new ContentDialog
        {
            Title = "添加工具",
            Content = panel,
            PrimaryButtonText = "添加",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = RootGrid.XamlRoot
        };

        browseFile.Click += async (_, _) =>
        {
            var picker = new FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
            picker.FileTypeFilter.Add(".exe");
            picker.FileTypeFilter.Add(".bat");
            picker.FileTypeFilter.Add(".msi");
            picker.FileTypeFilter.Add(".lnk");

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            pathBox.Text = file.Path;
            if (nameBox.Text.Length == 0)
                nameBox.Text = Path.GetFileNameWithoutExtension(file.Name);
        };

        browseFolder.Click += async (_, _) =>
        {
            var picker = new FolderPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
            picker.FileTypeFilter.Add("*");

            var folder = await picker.PickSingleFolderAsync();
            if (folder == null) return;

            pathBox.Text = folder.Path;
            if (nameBox.Text.Length == 0)
                nameBox.Text = folder.Name;
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        var source = pathBox.Text.Trim();
        if (source.Length == 0)
        {
            StatusTip.Text = "没有选择文件";
            return;
        }

        var category = categoryBox.SelectedItem as string;
        if (string.IsNullOrEmpty(category))
        {
            StatusTip.Text = "没有可用的分类";
            return;
        }

        var name = nameBox.Text.Trim();
        if (name.Length == 0) name = Path.GetFileNameWithoutExtension(source);

        var result = await Task.Run(() => ToolImporter.Import(
            _config, AppPaths.Root, source, category, name, descBox.Text.Trim()));

        if (!result.Success)
        {
            StatusTip.Text = result.Message;
            return;
        }

        BuildNavigation();
        RefreshList();
        StatusTip.Text = $"已添加「{name}」到 {category}";
    }

    // 悬停高亮：最上层那块 Border 自己变不透明即可，不需要动数据模型
    private void OnCardPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border layer) layer.Opacity = 1;
    }

    private void OnCardPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border layer) layer.Opacity = 0;
    }

    // ── 拖拽排序 ────────────────────────────────────
    /// <summary>
    /// GridView 自带 CanReorderItems 会把排序应用到 ItemsSource 上，
    /// 但不会写进 config.json，这里补上持久化。
    ///
    /// 只在「单个分类 + 未搜索」时放行：混合视图里拖动的语义不明确，
    /// 拖完还得猜它属于哪个分类，宁可不给拖。
    /// </summary>
    private void OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        var canReorder = _activeCategoryId != "all"
                         && string.IsNullOrWhiteSpace(SearchBox?.Text);

        if (!canReorder)
        {
            // 混合视图下 GridView 已经把顺序改了，重刷一次回到配置里的顺序
            if (!string.IsNullOrWhiteSpace(SearchBox?.Text) || _activeCategoryId == "all")
                RefreshList();
            return;
        }

        if (args.Items.Count == 0 || args.Items[0] is not ToolCardVm dragged) return;

        var index = Tools.IndexOf(dragged);
        if (index < 0) return;

        // 用相邻项作为锚点：往前挪就插到前一个之后，往后挪插到后一个之前
        var anchorIndex = index > 0 ? index - 1 : 1;
        if (anchorIndex < 0 || anchorIndex >= Tools.Count) return;

        var anchor = Tools[anchorIndex];
        var insertAfter = index > 0;

        if (_config.ReorderTool(_activeCategoryId, dragged.Id, anchor.Id, insertAfter))
            StatusTip.Text = $"已调整「{dragged.Name}」的位置";
    }

    // ── 平滑滚动 ────────────────────────────────────
    private const double WheelFactor = 0.5;   // 与 WPF 版一致：一格滚半格
    private const double WheelStep = 48;
    private ScrollViewer? _gridScroller;

    private void OnGridPointerWheel(object sender, PointerRoutedEventArgs e)
    {
        _gridScroller ??= FindDescendant<ScrollViewer>(ToolGrid);
        if (_gridScroller == null || _gridScroller.ScrollableHeight <= 0) return;

        var delta = e.GetCurrentPoint(ToolGrid).Properties.MouseWheelDelta;
        if (delta == 0) return;

        // 交给我们自己做，交给系统的话倍率和缓动都不可控
        e.Handled = true;

        var step = WheelStep * (delta / 120.0) * WheelFactor;
        var target = Math.Clamp(_gridScroller.VerticalOffset - step, 0, _gridScroller.ScrollableHeight);

        // disableAnimation:false —— 让 ScrollPresenter 走内建缓动，手感接近 WPF 版
        _gridScroller.ChangeView(null, target, null, false);
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) return match;

            var nested = FindDescendant<T>(child);
            if (nested != null) return nested;
        }

        return null;
    }
}

/// <summary>卡片绑定的最小视图模型：只暴露界面要用的东西</summary>
public sealed class ToolCardVm : INotifyPropertyChanged
{
    private bool _isSelected;
    private ImageSource? _icon;

    public ToolCardVm(ToolInfo tool)
    {
        Id = tool.Id;
        Name = tool.Name;
        Category = tool.Category;
        Description = tool.Description;
        RelativePath = tool.RelativePath;
    }

    public string Id { get; }
    public string Name { get; }
    public string Category { get; }
    public string Description { get; }
    public string RelativePath { get; }

    public ImageSource? Icon
    {
        get => _icon;
        set
        {
            if (ReferenceEquals(_icon, value)) return;
            _icon = value;
            Raise(nameof(Icon));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            Raise(nameof(SelectedVisibility));
        }
    }

    public Visibility SelectedVisibility => _isSelected ? Visibility.Visible : Visibility.Collapsed;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>回收站条目的展示模型</summary>
public sealed class TrashItemVm
{
    public TrashItemVm(string id, string name, string subtitle)
    {
        Id = id;
        Name = name;
        Subtitle = subtitle;
    }

    public string Id { get; }
    public string Name { get; }
    public string Subtitle { get; }
}
