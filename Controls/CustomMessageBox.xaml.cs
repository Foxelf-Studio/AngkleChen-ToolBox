using System.Windows;

namespace 陈叔叔工具箱.Controls;

public partial class CustomMessageBox : Window
{
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

    public CustomMessageBox(string message, string title = "提示", MessageBoxButton buttons = MessageBoxButton.OK)
    {
        InitializeComponent();
        TxtTitle.Text = title;
        TxtMessage.Text = message;

        switch (buttons)
        {
            case MessageBoxButton.OK:
                BtnOk.Visibility = Visibility.Visible;
                break;
            case MessageBoxButton.YesNo:
                BtnYes.Visibility = Visibility.Visible;
                BtnNo.Visibility = Visibility.Visible;
                break;
        }
    }

    private void OnYesClick(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.Yes;
        DialogResult = true;
        Close();
    }

    private void OnNoClick(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.No;
        DialogResult = false;
        Close();
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.OK;
        DialogResult = true;
        Close();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public static MessageBoxResult Show(string message, string title = "提示", MessageBoxButton buttons = MessageBoxButton.OK)
    {
        var dialog = new CustomMessageBox(message, title, buttons);
        dialog.ShowDialog();
        return dialog.Result;
    }
}
