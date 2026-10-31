using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace 陈叔叔工具箱.Controls;

public partial class ToolCard : UserControl
{
    public static readonly DependencyProperty ClickCommandProperty =
        DependencyProperty.Register(nameof(ClickCommand), typeof(ICommand), typeof(ToolCard));

    public static readonly DependencyProperty SelectCommandProperty =
        DependencyProperty.Register(nameof(SelectCommand), typeof(ICommand), typeof(ToolCard));

    public static readonly DependencyProperty EditCommandProperty =
        DependencyProperty.Register(nameof(EditCommand), typeof(ICommand), typeof(ToolCard));

    public static readonly DependencyProperty DeleteCommandProperty =
        DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(ToolCard));

    public static readonly DependencyProperty IsCardSelectedProperty =
        DependencyProperty.Register(nameof(IsCardSelected), typeof(bool), typeof(ToolCard),
            new PropertyMetadata(false, OnIsCardSelectedChanged));

    public ICommand? ClickCommand
    {
        get => (ICommand?)GetValue(ClickCommandProperty);
        set => SetValue(ClickCommandProperty, value);
    }

    public ICommand? SelectCommand
    {
        get => (ICommand?)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    public ICommand? EditCommand
    {
        get => (ICommand?)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public ICommand? DeleteCommand
    {
        get => (ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public bool IsCardSelected
    {
        get => (bool)GetValue(IsCardSelectedProperty);
        set => SetValue(IsCardSelectedProperty, value);
    }

    private Storyboard? _hoverIn, _hoverOut, _selectIn, _selectOut;

    public ToolCard()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        _hoverIn = FindResource("HoverIn") as Storyboard;
        _hoverOut = FindResource("HoverOut") as Storyboard;
        _selectIn = FindResource("SelectIn") as Storyboard;
        _selectOut = FindResource("SelectOut") as Storyboard;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (DataContext is Models.ToolInfo tool)
            ToolIcon.Source = Helpers.IconHelper.GetIcon(tool.RelativePath);
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (!IsCardSelected)
            _hoverIn?.Begin();
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (!IsCardSelected)
            _hoverOut?.Begin();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not Models.ToolInfo tool) return;

        if (e.ChangedButton == MouseButton.Right)
        {
            ShowWin11Menu(tool, e);
            e.Handled = true;
            return;
        }

        _dragStart = e.GetPosition(this);
        _dragArmed = e.ChangedButton == MouseButton.Left;

        if (e.ClickCount == 1)
        {
            SelectCommand?.Execute(tool);
        }
        else if (e.ClickCount == 2)
        {
            _dragArmed = false;   // 双击是启动，不要变成拖拽
            ClickCommand?.Execute(tool);
        }

        e.Handled = true;
    }

    // ── 拖拽排序 ─────────────────────────────────
    private Point _dragStart;
    private bool _dragArmed;

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_dragArmed || e.LeftButton != MouseButtonState.Pressed) return;

        var current = e.GetPosition(this);
        if (Math.Abs(current.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        _dragArmed = false;
        if (DataContext is Models.ToolInfo tool)
            DragDrop.DoDragDrop(this, tool, DragDropEffects.Move);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        _dragArmed = false;
    }

    // ── Win11 风格右键菜单 ──────────────────────────
    private void ShowWin11Menu(Models.ToolInfo tool, MouseButtonEventArgs e)
    {
        Win11ContextMenu.Show(PointToScreen(e.GetPosition(this)),
            new MenuAction("打开工具", "M 4 2 L 12 8 L 4 14 Z",
                () => ClickCommand?.Execute(tool)),
            MenuAction.Separator,
            new MenuAction("打开所在文件夹",
                "M 1 3 L 1 15 L 15 15 L 15 5 L 7 5 L 5 3 Z M 1 3 L 5 3 L 7 5 L 15 5",
                () => OpenContainingFolder(tool)),
            new MenuAction("复制完整路径",
                "M 5 1 L 5 11 L 13 11 L 13 3 L 7 3 M 3 3 L 3 15 L 11 15",
                () => CopyFullPath(tool)),
            MenuAction.Separator,
            new MenuAction("编辑名称与描述",
                "M 3 17.25 V 21 h 3.75 L 17.81 9.94 l -3.75 -3.75 L 3 17.25 Z M 20.71 7.04 c 0.39 -0.39 0.39 -1.02 0 -1.41 l -2.34 -2.34 c -0.39 -0.39 -1.02 -0.39 -1.41 0 l -1.83 1.83 3.75 3.75 1.83 -1.83 Z",
                () => EditCommand?.Execute(tool)),
            new MenuAction("删除工具",
                "M 3 3 L 5 3 L 5 1 L 11 1 L 11 3 L 13 3 L 13 5 L 3 5 Z M 4 5 L 12 5 L 11 15 L 5 15 Z",
                () => ConfirmDelete(tool), IsDestructive: true));
    }

    private static string GetFullPath(Models.ToolInfo tool)
        => System.IO.Path.Combine(Helpers.AppPaths.Root, tool.RelativePath);

    private static void OpenContainingFolder(Models.ToolInfo tool)
    {
        var folder = System.IO.Path.GetDirectoryName(GetFullPath(tool));
        if (folder != null && System.IO.Directory.Exists(folder))
            System.Diagnostics.Process.Start("explorer.exe", $"\"{folder}\"");
    }

    private static void CopyFullPath(Models.ToolInfo tool)
    {
        try { Clipboard.SetText(GetFullPath(tool)); }
        catch { /* 剪贴板被占用时忽略 */ }
    }

    private void ConfirmDelete(Models.ToolInfo tool)
    {
        var result = CustomMessageBox.Show(
            $"确定要删除工具 \"{tool.Name}\" 吗？\n\n文件会移入工具箱回收站，可在「设置 → 回收站」中恢复。",
            "确认删除",
            MessageBoxButton.YesNo);

        if (result == MessageBoxResult.Yes)
            DeleteCommand?.Execute(tool);
    }

    private static void OnIsCardSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToolCard card)
        {
            if ((bool)e.NewValue)
                card._selectIn?.Begin();
            else
                card._selectOut?.Begin();
        }
    }
}
