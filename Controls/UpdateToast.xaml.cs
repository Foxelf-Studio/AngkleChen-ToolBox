using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
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

        // 定位到主窗口右上角（标题栏下方，紧贴工具描述）
        UpdatePosition();

        // 监听主窗口移动和大小变化
        owner.LocationChanged += (_, _) => UpdatePosition();
        owner.SizeChanged += (_, _) => UpdatePosition();

        // 监听主窗口状态变化（最小化/恢复）
        owner.StateChanged += (_, _) =>
        {
            if (owner.WindowState == WindowState.Minimized)
            {
                Hide(); // 主窗口最小化时隐藏气泡
            }
            else
            {
                Show(); // 主窗口恢复时显示气泡
                UpdatePosition();
            }
        };

        // 淡入动画
        Opacity = 0;
        Loaded += (_, _) =>
        {
            // 设置圆角区域
            SetRoundedRegion(8);

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

        // 主窗口右上角，紧贴标题栏下方
        Left = Owner.Left + Owner.Width - Width - 16;
        Top = Owner.Top + 50; // 标题栏高度42 + 8边距
    }

    private void SetRoundedRegion(int radius)
    {
        var helper = new WindowInteropHelper(this);
        var rect = new RECT(0, 0, (int)ActualWidth, (int)ActualHeight);
        var hRgn = CreateRoundRectRgn(rect.Left, rect.Top, rect.Right + 1, rect.Bottom + 1, radius * 2, radius * 2);
        SetWindowRgn(helper.Handle, hRgn, true);
        DeleteObject(hRgn);
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

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
        public RECT(int left, int top, int right, int bottom)
        {
            Left = left; Top = top; Right = right; Bottom = bottom;
        }
    }
}
