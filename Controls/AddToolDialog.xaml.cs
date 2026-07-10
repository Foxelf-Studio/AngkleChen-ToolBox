using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace 陈叔叔工具箱.Controls;

public partial class AddToolDialog : Window
{
    private readonly string _toolboxRoot;
    private string? _selectedPath;
    private bool _isFolder;
    private bool _isDropDownOpen;
    private string? _selectedExePath;

    public string? ToolName { get; private set; }
    public string? ToolDescription { get; private set; }
    public string? CategoryName { get; private set; }
    public string? SourcePath { get; private set; }

    public AddToolDialog(string toolboxRoot, string[] existingCategories)
    {
        InitializeComponent();
        _toolboxRoot = toolboxRoot;

        // 填充分类列表
        CategoryListBox.ItemsSource = existingCategories;
        if (existingCategories.Length > 0)
            CategoryListBox.SelectedIndex = 0;

        // 绑定事件
        RadioFolder.Checked += (_, _) => { _isFolder = true; };
        RadioFile.Checked += (_, _) => { _isFolder = false; };
        RadioExistingCategory.Checked += (_, _) =>
        {
            ComboBorder.Visibility = Visibility.Visible;
            NewCategoryBorder.Visibility = Visibility.Collapsed;
        };
        RadioNewCategory.Checked += (_, _) =>
        {
            ComboBorder.Visibility = Visibility.Collapsed;
            NewCategoryBorder.Visibility = Visibility.Visible;
        };

        // 打开动画
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

    private void TitleBar_Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) return;
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void OnComboClick(object sender, MouseButtonEventArgs e)
    {
        _isDropDownOpen = !_isDropDownOpen;
        DropDownPanel.Visibility = _isDropDownOpen ? Visibility.Visible : Visibility.Collapsed;

        // 展开时把窗口滚动到最底部
        if (_isDropDownOpen)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(this);
                scrollViewer?.ScrollToBottom();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void OnCategorySelected(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryListBox.SelectedItem is string category)
        {
            TxtSelectedCategory.Text = category;
            _isDropDownOpen = false;
            DropDownPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        if (_isFolder)
        {
            // 使用 COM 互操作打开文件夹选择对话框
            var dialog = (IFileDialog)new FileOpenDialogRCW();
            dialog.GetOptions(out var options);
            dialog.SetOptions(options | FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM | FOS.FOS_PATHMUSTEXIST);

            var hr = dialog.Show(IntPtr.Zero);
            if (hr == 0)
            {
                dialog.GetResult(out var result);
                result.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var path);
                _selectedPath = path;

                // 让用户选择exe文件
                var exeFiles = Directory.GetFiles(path, "*.exe", SearchOption.TopDirectoryOnly);
                if (exeFiles.Length == 0)
                {
                    TxtSelectedPath.Text = path;
                    TxtSelectedPath.Foreground = System.Windows.Media.Brushes.White;
                    TxtToolName.Text = Path.GetFileName(path);
                }
                else if (exeFiles.Length == 1)
                {
                    _selectedExePath = exeFiles[0];
                    TxtSelectedPath.Text = $"{path} ({Path.GetFileName(_selectedExePath)})";
                    TxtSelectedPath.Foreground = System.Windows.Media.Brushes.White;
                    TxtToolName.Text = Path.GetFileNameWithoutExtension(_selectedExePath);
                }
                else
                {
                    // 多个exe，让用户选择
                    var selectDialog = new SelectExeDialog(exeFiles);
                    if (selectDialog.ShowDialog() == true)
                    {
                        _selectedExePath = selectDialog.SelectedExePath;
                        TxtSelectedPath.Text = $"{path} ({Path.GetFileName(_selectedExePath)})";
                        TxtSelectedPath.Foreground = System.Windows.Media.Brushes.White;
                        TxtToolName.Text = Path.GetFileNameWithoutExtension(_selectedExePath);
                    }
                    else
                    {
                        _selectedPath = null;
                        TxtSelectedPath.Text = "请选择工具路径...";
                        TxtSelectedPath.Foreground = System.Windows.Media.Brushes.Gray;
                    }
                }
            }
        }
        else
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择工具文件",
                Filter = "可执行文件 (*.exe)|*.exe|批处理文件 (*.bat;*.cmd)|*.bat;*.cmd|所有文件 (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                _selectedPath = dialog.FileName;
                TxtSelectedPath.Text = _selectedPath;
                TxtSelectedPath.Foreground = System.Windows.Media.Brushes.White;
                TxtToolName.Text = Path.GetFileNameWithoutExtension(_selectedPath);
            }
        }
    }

    private async void OnFinishClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedPath))
        {
            CustomMessageBox.Show("请先选择要添加的文件或文件夹。", "提示");
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtToolName.Text))
        {
            CustomMessageBox.Show("请输入工具名称。", "提示");
            return;
        }

        string category;
        if (RadioNewCategory.IsChecked == true)
        {
            if (string.IsNullOrWhiteSpace(TxtNewCategory.Text))
            {
                CustomMessageBox.Show("请输入新分类名称。", "提示");
                return;
            }
            category = TxtNewCategory.Text.Trim();
        }
        else
        {
            if (CategoryListBox.SelectedItem == null)
            {
                CustomMessageBox.Show("请选择一个分类。", "提示");
                return;
            }
            category = CategoryListBox.SelectedItem.ToString()!;
        }

        ToolName = TxtToolName.Text.Trim();
        ToolDescription = TxtToolDesc.Text.Trim();
        CategoryName = category;
        SourcePath = _selectedPath;

        // 计算目标路径
        var destDir = Path.Combine(_toolboxRoot, "工具", category, Path.GetFileName(_selectedPath));

        // 禁用按钮
        BtnFinish.IsEnabled = false;

        // 显示进度条对话框
        var progressDialog = new ProgressDialog(_selectedPath, destDir, _isFolder)
        {
            Owner = Owner
        };

        if (progressDialog.ShowDialog() == true)
        {
            // 复制成功，设置工具路径
            if (_isFolder && _selectedExePath != null)
            {
                // 使用用户选择的exe路径
                var relativeExePath = Path.GetRelativePath(_selectedPath, _selectedExePath);
                SourcePath = Path.Combine(destDir, relativeExePath);
            }
            else
            {
                SourcePath = destDir;
            }

            DialogResult = true;
            Close();
        }
        else
        {
            // 复制失败或取消
            BtnFinish.IsEnabled = true;
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        CloseWithAnimation();
    }

    private void CloseWithAnimation()
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
        fadeOut.Completed += (_, _) =>
        {
            DialogResult = false;
            Close();
        };
        BeginAnimation(OpacityProperty, fadeOut);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T result) return result;
            var found = FindVisualChild<T>(child);
            if (found != null) return found;
        }
        return null;
    }
}

// COM 互操作定义
[ComImport, Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
internal class FileOpenDialogRCW { }

[ComImport, Guid("42f85136-db7e-439c-85f1-e4075d135fc8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileDialog
{
    [PreserveSig] int Show(IntPtr hwndOwner);
    void SetFileTypes();
    void SetFileTypeIndex();
    void GetFileTypeIndex();
    void Advise();
    void Unadvise();
    void SetOptions(FOS fos);
    void GetOptions(out FOS pfos);
    void SetDefaultFolder();
    void SetFolder();
    void GetFolder();
    void GetCurrentSelection();
    void SetFileName();
    void GetFileName();
    void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
    void SetOkButtonLabel();
    void SetFileNameLabel();
    void GetResult(out IShellItem ppsi);
    void AddPlace();
    void SetDefaultExtension();
    void Close();
    void SetClientGuid();
    void ClearClientData();
    void SetFilter();
    void GetResults();
    void GetSelectedItems();
}

[ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItem
{
    void BindToHandler();
    void GetParent();
    void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
    void GetAttributes();
    void Compare();
}

internal enum SIGDN : uint
{
    SIGDN_FILESYSPATH = 0x80058000,
}

[Flags]
internal enum FOS
{
    FOS_OVERWRITEPROMPT = 0x2,
    FOS_STRICTFILETYPES = 0x4,
    FOS_NOCHANGEDIR = 0x8,
    FOS_PICKFOLDERS = 0x20,
    FOS_FORCEFILESYSTEM = 0x40,
    FOS_ALLNONSTORAGEITEMS = 0x80,
    FOS_NOVALIDATE = 0x100,
    FOS_ALLOWMULTISELECT = 0x200,
    FOS_PATHMUSTEXIST = 0x800,
    FOS_FILEMUSTEXIST = 0x1000,
    FOS_CREATEPROMPT = 0x2000,
    FOS_SHAREAWARE = 0x4000,
    FOS_NOREADONLYRETURN = 0x8000,
    FOS_NOTESTFILECREATE = 0x10000,
    FOS_HIDEMRUPLACES = 0x20000,
    FOS_HIDEPINNEDPLACES = 0x40000,
    FOS_NODEREFERENCELINKS = 0x100000,
    FOS_DONTADDTORECENT = 0x2000000,
    FOS_FORCESHOWHIDDEN = 0x10000000,
    FOS_DEFAULTNOMINIMODE = 0x20000000,
    FOS_FORCEPREVIEWPANEON = 0x40000000,
}
