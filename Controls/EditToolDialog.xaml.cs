using System.Windows;
using 陈叔叔工具箱.Models;

namespace 陈叔叔工具箱.Controls;

/// <summary>
/// 编辑工具的名称与描述。
/// 刻意不支持改路径：移动文件风险高，改名不该牵连文件系统。
/// </summary>
public partial class EditToolDialog : Window
{
    public string ToolName { get; private set; } = "";
    public string ToolDescription { get; private set; } = "";

    public EditToolDialog(ToolInfo tool)
    {
        InitializeComponent();

        TxtPath.Text = tool.RelativePath;
        TxtName.Text = tool.Name;
        TxtDesc.Text = tool.Description;

        ToolName = tool.Name;
        ToolDescription = tool.Description;

        Loaded += (_, _) =>
        {
            TxtName.Focus();
            TxtName.SelectAll();
        };
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var name = TxtName.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            CustomMessageBox.Show("名称不能为空。", "提示");
            TxtName.Focus();
            return;
        }

        ToolName = name;
        ToolDescription = TxtDesc.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
