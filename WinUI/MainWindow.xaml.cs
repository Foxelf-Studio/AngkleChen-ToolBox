using System.Collections.ObjectModel;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using 陈叔叔工具箱.Helpers;

namespace AngkleChen;

public sealed partial class MainWindow : Window
{
    private readonly AppConfig _config;
    private string _activeCategoryId = "all";

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

        if (_config.RestoredFromTemplate)
            StatusTip.Text = "config.json 缺失或损坏，已用内置清单重建";

        BuildNavigation();
        ShowCategory("all");
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
        _activeCategoryId = categoryId;
        RefreshList();
    }

    private void RefreshList()
    {
        var query = SearchBox?.Text?.Trim() ?? "";

        var source = _config.GetAllTools().AsEnumerable();

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

        Tools.Clear();
        foreach (var tool in list)
            Tools.Add(new ToolCardVm(tool.Name, tool.Category, tool.Id));

        ToolGrid.ItemsSource = Tools;

        var active = _activeCategoryId == "all"
            ? null
            : _config.Categories.FirstOrDefault(c => c.Id == _activeCategoryId);

        HeaderTitle.Text = active?.Name ?? "全部工具";
        HeaderSubtitle.Text = query.Length > 0
            ? $"搜索「{query}」的结果"
            : $"共 {list.Count} 款工具";

        StatusText.Text = $"共 {list.Count} 款工具 · v{VersionInfo.Current}";
    }

    private void OnCategoryChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem { Tag: string id })
            ShowCategory(id);
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e) => RefreshList();

    private void OnToolClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ToolCardVm vm)
            StatusTip.Text = $"已选择「{vm.Name}」（启动逻辑将在 P1 接入）";
    }
}

/// <summary>卡片绑定的最小视图模型，P1 会补图标、描述、存在性</summary>
public sealed class ToolCardVm
{
    public ToolCardVm(string name, string category, string id)
    {
        Name = name;
        Category = category;
        Id = id;
    }

    public string Name { get; }
    public string Category { get; }
    public string Id { get; }
}
