using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;

namespace 陈叔叔工具箱.Controls;

public partial class UpdateToast : Window
{
    private readonly string _releaseUrl;

    public UpdateToast(string releaseUrl, Window owner)
    {
        InitializeComponent();
        _releaseUrl = releaseUrl;
        Owner = owner;

        // 定位到主窗口右上角（标题栏下方）
        UpdatePosition();

        // 监听主窗口移动
        owner.LocationChanged += (_, _) => UpdatePosition();
        owner.SizeChanged += (_, _) => UpdatePosition();

        // 淡入动画
        Opacity = 0;
        Loaded += (_, _) =>
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 2 }
            };
            BeginAnimation(OpacityProperty, fadeIn);
        };
    }

    private void UpdatePosition()
    {
        if (Owner == null) return;

        // 主窗口右上角，向内偏移
        Left = Owner.Left + Owner.Width - Width - 16;
        Top = Owner.Top + 58; // 标题栏高度42 + 16边距
    }

    private void OnViewUpdateClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(_releaseUrl)
        {
            UseShellExecute = true
        });
        Close();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
        fadeOut.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, fadeOut);
    }
}
