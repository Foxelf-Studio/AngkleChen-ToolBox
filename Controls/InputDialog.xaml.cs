using System.Windows;
using System.Windows.Input;

namespace 陈叔叔工具箱.Controls;

/// <summary>
/// 通用单行文本输入框（目前用于分类重命名）。
/// </summary>
public partial class InputDialog : Window
{
    public string Value { get; private set; } = "";

    public InputDialog(string title, string label, string initialValue, string hint = "")
    {
        InitializeComponent();

        Title = title;
        TxtLabel.Text = label;
        TxtValue.Text = initialValue;
        TxtHint.Text = hint;
        TxtHint.Visibility = string.IsNullOrEmpty(hint) ? Visibility.Collapsed : Visibility.Visible;

        Loaded += (_, _) =>
        {
            TxtValue.Focus();
            TxtValue.SelectAll();
        };
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        var value = TxtValue.Text.Trim();

        if (value.Length == 0)
        {
            CustomMessageBox.Show("名称不能为空。", "提示");
            TxtValue.Focus();
            return;
        }

        Value = value;
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnValueKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }
}
