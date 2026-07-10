using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;

namespace 陈叔叔工具箱.Controls;

public partial class UpdateToast : Window
{
    private readonly string _releaseUrl;

    public UpdateToast(string releaseUrl)
    {
        InitializeComponent();
        _releaseUrl = releaseUrl;

        // 定位到右上角
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 16;
        Top = workArea.Top + 16;

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
