using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace 陈叔叔工具箱.Controls;

/// <summary>
/// 右键菜单的一项
/// </summary>
public sealed record MenuAction(string Text, string IconPath, Action OnClick, bool IsDestructive = false)
{
    /// <summary>分隔线</summary>
    public static MenuAction Separator { get; } = new("", "", () => { });
}

/// <summary>
/// Win11 风格右键菜单。
/// 此前 ToolCard 和 MainWindow 各有一份完全相同的实现（约 250 行重复代码），
/// 抽取后全程序只保留一个活动菜单实例。
/// </summary>
public static class Win11ContextMenu
{
    private const double MenuWidth = 224;

    // 全局只允许存在一个菜单
    private static Window? _activeMenu;
    private static bool _isClosing;

    /// <summary>
    /// 在屏幕坐标处弹出菜单
    /// </summary>
    public static void Show(Point screenPoint, params MenuAction[] items)
    {
        CloseActive();

        var menu = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = false,   // 用 SetWindowRgn 做圆角，避免透明窗口的边缘回绕
            Background = new SolidColorBrush(Color.FromRgb(0x2b, 0x2b, 0x2b)),
            Width = MenuWidth,
            SizeToContent = SizeToContent.Height,
            ShowInTaskbar = false,
            Topmost = true,
            Left = screenPoint.X + 8,
            Top = screenPoint.Y - 10,
            ResizeMode = ResizeMode.NoResize,
            Opacity = 0
        };

        menu.Loaded += (_, _) =>
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(menu);
            var hRgn = CreateRoundRectRgn(0, 0, (int)menu.ActualWidth + 1, (int)menu.ActualHeight + 1, 16, 16);
            SetWindowRgn(helper.Handle, hRgn, true);
            DeleteObject(hRgn);
        };

        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x2b, 0x2b, 0x2b)),
            CornerRadius = new CornerRadius(8),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xff, 0xff, 0xff)),
            BorderThickness = new Thickness(1)
        };

        var stack = new StackPanel { Margin = new Thickness(4) };
        foreach (var item in items)
        {
            stack.Children.Add(item.Text.Length == 0 && item.IconPath.Length == 0
                ? CreateSeparator()
                : CreateItem(item, () => CloseWithAnimation(menu)));
        }

        border.Child = stack;
        menu.Content = border;

        menu.Deactivated += (_, _) => CloseWithAnimation(menu);
        menu.Closed += (_, _) =>
        {
            if (_activeMenu == menu) _activeMenu = null;
        };

        menu.Show();
        _activeMenu = menu;

        // 不超出工作区
        menu.UpdateLayout();
        var workArea = SystemParameters.WorkArea;
        if (menu.Left + menu.Width > workArea.Right)
            menu.Left = screenPoint.X - menu.Width - 8;
        if (menu.Top + menu.Height > workArea.Bottom)
            menu.Top = workArea.Bottom - menu.Height - 8;

        menu.BeginAnimation(Window.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 2 }
        });
    }

    /// <summary>立即关闭当前菜单（不带动画）</summary>
    public static void CloseActive()
    {
        if (_activeMenu == null) return;

        var menu = _activeMenu;
        _activeMenu = null;
        try { menu.Close(); } catch { /* 已关闭 */ }
    }

    private static void CloseWithAnimation(Window menu)
    {
        if (_isClosing) return;
        _isClosing = true;

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(100));
        fadeOut.Completed += (_, _) =>
        {
            try { menu.Close(); } catch { }
            if (_activeMenu == menu) _activeMenu = null;
            _isClosing = false;
        };
        menu.BeginAnimation(Window.OpacityProperty, fadeOut);
    }

    private static Border CreateItem(MenuAction action, Action closeMenu)
    {
        var bgBrush = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x2d));
        var item = new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 8, 10, 8),
            Cursor = Cursors.Hand,
            Background = bgBrush
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var icon = new Path
        {
            Data = Geometry.Parse(action.IconPath),
            Fill = new SolidColorBrush(action.IsDestructive ? Color.FromRgb(0xff, 0x6b, 0x6b) : Color.FromRgb(0xaa, 0xaa, 0xaa)),
            Width = 14,
            Height = 14,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Grid.SetColumn(icon, 0);

        var text = new TextBlock
        {
            Text = action.Text,
            FontFamily = new FontFamily("Microsoft YaHei"),
            FontSize = 12,
            Foreground = action.IsDestructive ? new SolidColorBrush(Color.FromRgb(0xff, 0x6b, 0x6b)) : Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };
        Grid.SetColumn(text, 1);

        grid.Children.Add(icon);
        grid.Children.Add(text);
        item.Child = grid;

        var hoverColor = Color.FromRgb(0x35, 0x35, 0x35);
        var normalColor = Color.FromRgb(0x2d, 0x2d, 0x2d);

        item.MouseEnter += (_, _) =>
            bgBrush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(hoverColor, TimeSpan.FromMilliseconds(150)));
        item.MouseLeave += (_, _) =>
            bgBrush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(normalColor, TimeSpan.FromMilliseconds(150)));

        item.PreviewMouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            closeMenu();          // 先关菜单再做动作，避免动作弹出对话框时菜单还在
            action.OnClick();
        };

        return item;
    }

    private static Border CreateSeparator() => new()
    {
        Height = 1,
        Background = new SolidColorBrush(Color.FromArgb(0x1a, 0xff, 0xff, 0xff)),
        Margin = new Thickness(10, 4, 10, 4)
    };

    // ── 圆角窗口区域 ──────────────────────────────
    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);
}
