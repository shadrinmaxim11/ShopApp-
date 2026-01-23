using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ShopApp;

public partial class ClientHomeWindow : Window
{
    public ClientHomeWindow()
    {
        InitializeComponent();
        BackButton.Click += BackButton_Click;
        ProductsButton.Click += ProductsButton_Click;
    }

    private void ProductsButton_Click(object? sender, RoutedEventArgs e)
    {
        var products = new ClientWindow();
        products.Show();
        Close();
    }

    private void BackButton_Click(object? sender, RoutedEventArgs e)
    {
        var main = new MainWindow();
        main.Show();
        Close();
    }
}

