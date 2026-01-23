using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ShopApp;

public partial class AdminWindow : Window
{
    public AdminWindow()
    {
        InitializeComponent();
        BackButton.Click += BackButton_Click;
        ProductsButton.Click += ProductsButton_Click;
        OrdersButton.Click += OrdersButton_Click;
    }

    private void BackButton_Click(object? sender, RoutedEventArgs e)
    {
        var main = new MainWindow();
        main.Show();
        Close();
    }

    private void ProductsButton_Click(object? sender, RoutedEventArgs e)
    {
        var products = new AdminProductsWindow();
        products.Show();
        Close();
    }

    private void OrdersButton_Click(object? sender, RoutedEventArgs e)
    {
        var orders = new OrdersWindow(true, OrdersCaller.Admin);
        orders.Show();
        Close();
    }
}

