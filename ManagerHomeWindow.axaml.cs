using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ShopApp;

public partial class ManagerHomeWindow : Window
{
    public ManagerHomeWindow()
    {
        InitializeComponent();
        BackButton.Click += BackButton_Click;
        ProductsButton.Click += ProductsButton_Click;
        OrdersButton.Click += OrdersButton_Click;
    }

    private void ProductsButton_Click(object? sender, RoutedEventArgs e)
    {
        var products = new ManagerWindow();
        products.Show();
        Close();
    }

    private void OrdersButton_Click(object? sender, RoutedEventArgs e)
    {
        var orders = new OrdersWindow(false, OrdersCaller.Manager);
        orders.Show();
        Close();
    }

    private void BackButton_Click(object? sender, RoutedEventArgs e)
    {
        var main = new MainWindow();
        main.Show();
        Close();
    }
}

