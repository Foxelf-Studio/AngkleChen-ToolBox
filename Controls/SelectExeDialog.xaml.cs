using System.IO;
using System.Windows;
using System.Windows.Input;

namespace 陈叔叔工具箱.Controls;

public partial class SelectExeDialog : Window
{
    public string? SelectedExePath { get; private set; }

    public SelectExeDialog(string[] exeFiles)
    {
        InitializeComponent();

        // 填充exe文件列表
        ExeListBox.ItemsSource = exeFiles.Select(f => Path.GetFileName(f)).ToArray();
        ExeListBox.SelectedIndex = 0;

        // 保存完整路径
        _exePaths = exeFiles;
    }

    private readonly string[] _exePaths;

    private void OnSelectClick(object sender, RoutedEventArgs e)
    {
        if (ExeListBox.SelectedIndex >= 0)
        {
            SelectedExePath = _exePaths[ExeListBox.SelectedIndex];
            DialogResult = true;
            Close();
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TitleBar_Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }
}
