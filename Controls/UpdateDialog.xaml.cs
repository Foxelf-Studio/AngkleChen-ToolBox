using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using 陈叔叔工具箱.Helpers;

namespace 陈叔叔工具箱.Controls;

public partial class UpdateDialog : Window
{
    private readonly UpdateCheckResult _result;

    public UpdateDialog(UpdateCheckResult result)
    {
        InitializeComponent();
        _result = result;

        TxtCurrentVersion.Text = $"v{result.CurrentVersion}";
        TxtLatestVersion.Text = $"v{result.LatestVersion}";
        TxtChangelog.Text = string.IsNullOrEmpty(result.Changelog) ? "暂无更新说明" : result.Changelog;

        // 播放弹入动画
        Loaded += (_, _) => PlayEnterAnimation();
    }

    private void PlayEnterAnimation()
    {
        var sb = new Storyboard();

        var scaleX = new DoubleAnimation(0.9, 1, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 3 }
        };
        Storyboard.SetTarget(scaleX, this);
        Storyboard.SetTargetProperty(scaleX,
            new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));

        var scaleY = new DoubleAnimation(0.9, 1, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 3 }
        };
        Storyboard.SetTarget(scaleY, this);
        Storyboard.SetTargetProperty(scaleY,
            new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
        {
            EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 2 }
        };
        Storyboard.SetTarget(fade, this);
        Storyboard.SetTargetProperty(fade, new PropertyPath(UIElement.OpacityProperty));

        sb.Children.Add(scaleX);
        sb.Children.Add(scaleY);
        sb.Children.Add(fade);

        // 设置初始状态
        var transform = new ScaleTransform(0.9, 0.9);
        RenderTransform = transform;
        RenderTransformOrigin = new Point(0.5, 0.5);
        Opacity = 0;

        sb.Begin();
    }

    private void OnUpdateClick(object sender, RoutedEventArgs e)
    {
        // 打开 GitHub Release 页面
        if (!string.IsNullOrEmpty(_result.ReleaseUrl))
        {
            Process.Start(new ProcessStartInfo(_result.ReleaseUrl)
            {
                UseShellExecute = true
            });
        }
        DialogResult = true;
        Close();
    }

    private void OnRemindLaterClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }
}
