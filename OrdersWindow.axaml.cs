using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using ShopApp.Services;

namespace ShopApp;

public enum OrdersCaller
{
    Admin,
    Manager
}

public partial class OrdersWindow : Window
{
    private readonly DbService _db = new DbService();
    private readonly bool _allowCrud;
    private readonly OrdersCaller _caller;
    private int? _selectedOrderId;

    // Пустой конструктор нужен для Avalonia runtime loader / дизайнера
    public OrdersWindow() : this(false, OrdersCaller.Admin)
    {
    }

    public OrdersWindow(bool allowCrud, OrdersCaller caller)
    {
        _allowCrud = allowCrud;
        _caller = caller;
        InitializeComponent();
        BackButton.Click += BackButton_Click;
        AddButton.Click += AddButton_Click;
        EditButton.Click += EditButton_Click;
        DeleteButton.Click += DeleteButton_Click;

        AddButton.IsVisible = _allowCrud;
        EditButton.IsVisible = _allowCrud;
        DeleteButton.IsVisible = _allowCrud;

        Loaded += OrdersWindow_Loaded;
    }

    private async void OrdersWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        await LoadOrdersAsync();
    }

    private async Task LoadOrdersAsync()
    {
        var list = await Task.Run(() => _db.GetOrders().ToList());
        OrdersPanel.Children.Clear();
        
        // Сохраняем выделенный заказ перед очисткой
        var selectedId = _selectedOrderId;
        
        if (list.Count == 0)
        {
            OrdersPanel.Children.Add(new TextBlock { Text = "Заказы не найдены." });
            _selectedOrderId = null;
            return;
        }

        Border? selectedBorder = null;
        foreach (var o in list)
        {
            var card = CreateOrderCard(o);
            OrdersPanel.Children.Add(card);
            
            // Восстанавливаем выделение
            if (selectedId.HasValue && o.Id == selectedId.Value && card is Border border)
            {
                border.BorderBrush = Brushes.Blue;
                border.BorderThickness = new Thickness(2);
                _selectedOrderId = o.Id;
                selectedBorder = border;
            }
        }
    }

    private Control CreateOrderCard(OrderView o)
    {
        var panel = new StackPanel { Spacing = 3 };
        panel.Children.Add(new TextBlock { Text = $"Заказ №{o.Id} от {o.OrderDate:dd.MM.yyyy}", FontWeight = FontWeight.Bold });
        panel.Children.Add(new TextBlock { Text = $"Клиент: {o.UserLogin}" });
        panel.Children.Add(new TextBlock { Text = $"Пункт выдачи: {o.City}, {o.Street} {o.House}" });
        panel.Children.Add(new TextBlock { Text = $"Статус: {o.Status}" });
        
        // Показываем товары в заказе
        var items = _db.GetOrderItems(o.Id).ToList();
        if (items.Count > 0)
        {
            panel.Children.Add(new TextBlock { Text = "Товары:", FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 5, 0, 0) });
            foreach (var item in items)
            {
                panel.Children.Add(new TextBlock 
                { 
                    Text = $"  • {item.ProductName} ({item.Article}), размер {item.Size}, x{item.Quantity} = {item.Total:0.##} ₽",
                    Margin = new Thickness(10, 0, 0, 0)
                });
            }
        }
        
        panel.Children.Add(new TextBlock { Text = $"Итого: {o.TotalSum:0.##} ₽", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 5, 0, 0) });

        var border = new Border
        {
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(8),
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Child = panel,
            Tag = o.Id // Сохраняем ID заказа для выбора
        };
        
        // Делаем карточку кликабельной для выбора
        if (_allowCrud)
        {
            border.PointerPressed += (s, e) =>
            {
                // Сбрасываем выделение у всех карточек
                foreach (var child in OrdersPanel.Children)
                {
                    if (child is Border b)
                    {
                        b.BorderBrush = Brushes.Gray;
                        b.BorderThickness = new Thickness(1);
                    }
                }
                // Выделяем текущую
                border.BorderBrush = Brushes.Blue;
                border.BorderThickness = new Thickness(2);
                _selectedOrderId = o.Id;
            };
        }

        return border;
    }

    private async void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!_allowCrud) return;
        var dialog = new OrderEditDialog(_db, null);
        var result = await dialog.ShowDialog<OrderFormData?>(this);
        if (result is null) return;
        
        try
        {
            await Task.Run(() => _db.InsertOrder(result));
            _selectedOrderId = null; // Сбрасываем выделение после добавления
            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            await ShowMessageDialog("Ошибка", $"Не удалось добавить заказ: {ex.Message}");
        }
    }

    private int? GetSelectedOrderId()
    {
        return _selectedOrderId;
    }

    private async void EditButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!_allowCrud) return;
        var selectedId = GetSelectedOrderId();
        if (!selectedId.HasValue)
        {
            await ShowMessageDialog("Ошибка", "Выберите заказ для редактирования (кликните на карточку заказа)");
            return;
        }
        
        var selected = _db.GetOrders().FirstOrDefault(o => o.Id == selectedId.Value);
        if (selected is null) return;
        
        var dialog = new OrderEditDialog(_db, selected);
        var result = await dialog.ShowDialog<OrderFormData?>(this);
        if (result is null) return;
        
        try
        {
            await Task.Run(() => _db.UpdateOrder(selected.Id, result));
            _selectedOrderId = null;
            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            await ShowMessageDialog("Ошибка", $"Не удалось обновить заказ: {ex.Message}");
        }
    }

    private async void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!_allowCrud) return;
        var selectedId = GetSelectedOrderId();
        if (!selectedId.HasValue)
        {
            await ShowMessageDialog("Ошибка", "Выберите заказ для удаления (кликните на карточку заказа)");
            return;
        }
        
        try
        {
            await Task.Run(() => _db.DeleteOrder(selectedId.Value));
            _selectedOrderId = null;
            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            await ShowMessageDialog("Ошибка", $"Не удалось удалить заказ: {ex.Message}");
        }
    }

    private void BackButton_Click(object? sender, RoutedEventArgs e)
    {
        Window target = _caller switch
        {
            OrdersCaller.Admin => new AdminWindow(),
            OrdersCaller.Manager => new ManagerHomeWindow(),
            _ => new MainWindow()
        };
        target.Show();
        Close();
    }

    private async Task ShowMessageDialog(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        
        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0),
            Width = 80
        };
        okButton.Click += (s, e) => dialog.Close();
        
        dialog.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Children =
            {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                okButton
            }
        };
        
        await dialog.ShowDialog(this);
    }
}
