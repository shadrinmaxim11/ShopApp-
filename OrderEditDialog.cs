using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ShopApp.Services;

namespace ShopApp;

public class OrderEditDialog : Window
{
    private readonly ComboBox _userCombo = new();
    private readonly ComboBox _pointCombo = new();
    private readonly ComboBox _statusCombo = new();
    private readonly DatePicker _datePicker = new() { SelectedDate = DateTime.Today };
    private readonly StackPanel _itemsPanel = new() { Spacing = 5 };
    private readonly ScrollViewer _itemsScroll = new() { Height = 200 };
    private readonly DbService _db;
    private readonly OrderView? _existing;
    // product, size, quantity
    private readonly List<(ComboBox productCombo, TextBox sizeBox, TextBox qtyBox)> _itemControls = new();

    public OrderEditDialog(DbService db, OrderView? existing)
    {
        _db = db;
        _existing = existing;
        Title = existing == null ? "Добавить заказ" : "Редактировать заказ";
        Width = 520;
        Height = 600;

        var users = _db.GetUsers().ToList();
        var points = _db.GetPickupPoints().ToList();
        var statuses = _db.GetOrderStatuses().ToList();
        var products = _db.GetProducts().ToList();

        _userCombo.ItemsSource = users;
        _pointCombo.ItemsSource = points;
        _statusCombo.ItemsSource = statuses;

        if (existing != null)
        {
            _datePicker.SelectedDate = existing.OrderDate;
            _userCombo.SelectedItem = users.FirstOrDefault(u => u.Login == existing.UserLogin);
            _statusCombo.SelectedItem = statuses.FirstOrDefault(s => s.Name == existing.Status);
            _pointCombo.SelectedItem = points.FirstOrDefault(p => existing.City != null && existing.Street != null && (p.Display?.Contains(existing.City.Trim()) ?? false));

            // Загружаем существующие позиции заказа
            var existingItems = _db.GetOrderItems(existing.Id).ToList();
            foreach (var item in existingItems)
            {
                AddItemRow(products, item.ProductId, item.Size, item.Quantity);
            }
        }
        else
        {
            // Добавляем одну пустую строку для нового заказа
            AddItemRow(products);
        }

        _itemsScroll.Content = _itemsPanel;

        var addItemButton = new Button { Content = "+ Добавить товар", Width = 140 };
        addItemButton.Click += (_, _) => AddItemRow(products);

        var okButton = new Button { Content = "Сохранить", HorizontalAlignment = HorizontalAlignment.Right, Width = 100 };
        var cancelButton = new Button { Content = "Отмена", HorizontalAlignment = HorizontalAlignment.Left, Width = 100 };
        
        okButton.Click += (_, _) =>
        {
            if (_userCombo.SelectedItem is not SimpleUser u ||
                _pointCombo.SelectedItem is not PickupPoint p ||
                _statusCombo.SelectedItem is not OrderStatus s ||
                !_datePicker.SelectedDate.HasValue)
            {
                Close(null);
                return;
            }

            var items = new List<OrderItemData>();
            foreach (var (productCombo, sizeBox, qtyBox) in _itemControls)
            {
                if (productCombo.SelectedItem is Product prod &&
                    int.TryParse(sizeBox.Text, out var size) && size > 0 &&
                    int.TryParse(qtyBox.Text, out var qty) && qty > 0)
                {
                    items.Add(new OrderItemData { ProductId = prod.Id, Size = size, Quantity = qty });
                }
            }

            if (items.Count == 0)
            {
                Close(null);
                return;
            }

            var data = new OrderFormData
            {
                UserId = u.Id,
                PointId = p.Id,
                StatusId = s.Id,
                OrderDate = _datePicker.SelectedDate.Value.Date,
                Items = items
            };
            Close(data);
        };
        cancelButton.Click += (_, _) => Close(null);

        Content = new ScrollViewer
        {
            Content = new StackPanel
            {
                Margin = new Thickness(12),
                Spacing = 10,
                Children =
                {
                    new TextBlock{ Text="Клиент"},
                    _userCombo,
                    new TextBlock{ Text="Пункт выдачи"},
                    _pointCombo,
                    new TextBlock{ Text="Статус"},
                    _statusCombo,
                    new TextBlock{ Text="Дата"},
                    _datePicker,
                    new TextBlock{ Text="Товары в заказе", FontWeight = Avalonia.Media.FontWeight.Bold, Margin = new Thickness(0, 10, 0, 5)},
                    _itemsScroll,
                    addItemButton,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Margin = new Thickness(0, 10, 0, 0),
                        Children = { cancelButton, okButton }
                    }
                }
            }
        };
    }

    private void AddItemRow(List<Product> products, int? selectedProductId = null, int? size = null, int? quantity = null)
    {
        var productCombo = new ComboBox
        {
            ItemsSource = products,
            Width = 300,
            Margin = new Thickness(0, 0, 5, 0)
        };

        var sizeBox = new TextBox
        {
            Watermark = "Размер",
            Width = 80,
            Margin = new Thickness(0, 0, 5, 0),
            Text = size?.ToString() ?? ""
        };

        var qtyBox = new TextBox
        {
            Watermark = "Количество",
            Width = 100,
            Text = quantity?.ToString() ?? ""
        };

        if (selectedProductId.HasValue)
        {
            productCombo.SelectedItem = products.FirstOrDefault(p => p.Id == selectedProductId.Value);
        }

        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Children = { productCombo, sizeBox, qtyBox }
        };

        var removeButton = new Button { Content = "×", Width = 30 };
        removeButton.Click += (_, _) =>
        {
            _itemControls.Remove((productCombo, sizeBox, qtyBox));
            _itemsPanel.Children.Remove(panel);
        };
        
        panel.Children.Add(removeButton);

        _itemControls.Add((productCombo, sizeBox, qtyBox));
        _itemsPanel.Children.Add(panel);
    }
}
