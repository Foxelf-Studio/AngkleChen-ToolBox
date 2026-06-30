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

        if (e.ClickCount == 1)
            SelectCommand?.Execute(tool);
        else if (e.ClickCount == 2)
            ClickCommand?.Execute(tool);

        e.Handled = true;
    }

    // ── Win11 风格右键菜单 ──────────────────────────
    private static Window? _activeMenu;
    private static bool _isMenuClosing; // 防止重复关闭

    private void ShowWin11Menu(Models.ToolInfo tool, MouseButtonEventArgs e)
    {
        // 关闭已有菜单
        _activeMenu?.Close();
        _activeMenu = null;

        // 计算位置（光标右侧，用 WPF 原生方式）
        var screenPos = this.PointToScreen(e.GetPosition(this));

        // 创建菜单窗口
        var menu = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            Width = 220,
            SizeToContent = SizeToContent.Height,
            ShowInTaskbar = false,
            Topmost = true,
            Left = screenPos.X + 8,
            Top = screenPos.Y - 10,
            ResizeMode = ResizeMode.NoResize,
        };

        // 主容器（圆角背景）
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x2b, 0x2b, 0x2b)),
            CornerRadius = new CornerRadius(8),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xff, 0xff, 0xff)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(4),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                Opacity = 0.4,
                ShadowDepth = 2,
                BlurRadius = 12,
            },
        };

        var stack = new StackPanel { Margin = new Thickness(4) };

        // 关闭菜单的辅助方法（带渐隐动画）
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

        // 菜单项（Path 矢量图标）
        // 打开图标：右箭头
        stack.Children.Add(CreateMenuItem(
            "M 4 2 L 12 8 L 4 14 Z", "打开工具", () =>
        {
            CloseMenuWithAnimation(() => ClickCommand?.Execute(tool));
        }));

        stack.Children.Add(CreateMenuSeparator());

        // 文件夹图标
        stack.Children.Add(CreateMenuItem(
            "M 1 3 L 1 15 L 15 15 L 15 5 L 7 5 L 5 3 Z M 1 3 L 5 3 L 7 5 L 15 5",
            "打开所在文件夹", () =>
        {
            var root = AppDomain.CurrentDomain.BaseDirectory;
            var fullPath = System.IO.Path.Combine(root, tool.RelativePath);
            var folder = System.IO.Path.GetDirectoryName(fullPath);
            CloseMenuWithAnimation(() =>
            {
                if (folder != null && System.IO.Directory.Exists(folder))
                    System.Diagnostics.Process.Start("explorer.exe", $"\"{folder}\"");
            });
        }));

        // 复制图标
        stack.Children.Add(CreateMenuItem(
            "M 5 1 L 5 11 L 13 11 L 13 3 L 7 3 M 3 3 L 3 15 L 11 15",
            "复制完整路径", () =>
        {
            var root = AppDomain.CurrentDomain.BaseDirectory;
            var fullPath = System.IO.Path.Combine(root, tool.RelativePath);
            CloseMenuWithAnimation(() => Clipboard.SetText(fullPath));
        }));

        border.Child = stack;
        menu.Content = border;
        menu.Opacity = 0; // 初始透明

        // 失去焦点时关闭菜单（带渐隐动画）
        menu.Deactivated += (_, _) => CloseMenuWithAnimation();

        // 处理 Closed 事件确保清理
        menu.Closed += (_, _) =>
        {
            if (_activeMenu == menu)
                _activeMenu = null;
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

        // 淡入动画 — 与卡片动画风格统一
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 2 }
        };
        menu.BeginAnimation(Window.OpacityProperty, fadeIn);
    }

    private static Border CreateMenuItem(string pathData, string text, Action onClick)
    {
        // 初始背景色 = 卡片色 #2d2d2d
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

        // Path 矢量图标
        var iconPath = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse(pathData),
            Fill = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)),
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
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
        };
        Grid.SetColumn(nameText, 1);

        grid.Children.Add(iconPath);
        grid.Children.Add(nameText);
        item.Child = grid;

        // hover 动画 — 与卡片一致：ColorAnimation 0.15s 线性过渡
        var hoverColor = Color.FromRgb(0x35, 0x35, 0x35); // #353535
        var normalColor = Color.FromRgb(0x2d, 0x2d, 0x2d); // #2d2d2d

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

    private static Border CreateMenuSeparator()
    {
        return new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.FromArgb(0x1a, 0xff, 0xff, 0xff)),
            Margin = new Thickness(10, 4, 10, 4),
        };
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
