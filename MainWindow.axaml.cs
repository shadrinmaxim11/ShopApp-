using Avalonia.Controls;
using Avalonia.Interactivity;
using ShopApp.Services;

namespace ShopApp;

public partial class MainWindow : Window
{
    private readonly DbService _db = new DbService();

    public MainWindow()
    {
        InitializeComponent();
        LoginButton.Click += LoginButton_Click;
        GuestButton.Click += GuestButton_Click;
        BackButton.Click += BackButton_Click;
    }

    private void LoginButton_Click(object? sender, RoutedEventArgs e)
    {
        ClearError();

        var login = LoginTextBox.Text?.Trim() ?? string.Empty;
        var password = PasswordInput.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Введите логин и пароль.");
            return;
        }

        var user = _db.AuthenticateUser(login, password);
        if (user is null)
        {
            ShowError("Неверный логин или пароль.");
            return;
        }

        switch (user.RoleId)
        {
            case 1:
                OpenWindow(new AdminWindow());
                break;
            case 2:
                OpenWindow(new ManagerHomeWindow());
                break;
            case 3:
                OpenWindow(new ClientHomeWindow());
                break;
            default:
                ShowError("Неизвестная роль пользователя.");
                break;
        }
    }

    private void GuestButton_Click(object? sender, RoutedEventArgs e)
    {
        OpenWindow(new GuestWindow());
    }

    private void BackButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.IsVisible = true;
    }

    private void ClearError()
    {
        ErrorTextBlock.Text = string.Empty;
        ErrorTextBlock.IsVisible = false;
    }

    private void OpenWindow(Window window)
    {
        window.Show();
        Close();
    }
}
