using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ShopApp.Services;

namespace ShopApp;

public partial class GuestWindow : Window
{
    private readonly DbService _db = new DbService();
    private readonly List<Product> _products = new();

    public GuestWindow()
    {
        InitializeComponent();
        BackButton.Click += BackButton_Click;
        Loaded += GuestWindow_Loaded;
    }

    private async void GuestWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        await LoadProductsAsync();
        RenderProducts(_products);
    }

    private async Task LoadProductsAsync()
    {
        await Task.Run(() =>
        {
            _products.Clear();
            _products.AddRange(_db.GetProducts());
        });
    }

    private void RenderProducts(IEnumerable<Product> products)
    {
        ProductsPanel.Children.Clear();

        foreach (var p in products)
        {
            ProductsPanel.Children.Add(CreateProductCard(p));
        }

        if (!products.Any())
        {
            ProductsPanel.Children.Add(new TextBlock { Text = "Товары не найдены." });
        }
    }

    private Control CreateProductCard(Product p)
    {
        // Та же структура, что у админа: слева картинка, по центру инфо, справа "Скидка"
        var rootGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("180,*,100"),
            ColumnSpacing = 10
        };

        // Левая часть: изображение товара (или Icon.jpg)
        Bitmap? bmp = null;
        var imageCandidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(p.ImagePath))
        {
            imageCandidates.Add(p.ImagePath);
            imageCandidates.Add(Path.Combine("Images", p.ImagePath));
            imageCandidates.Add(Path.Combine(AppContext.BaseDirectory ?? string.Empty, p.ImagePath));
            imageCandidates.Add(Path.Combine(AppContext.BaseDirectory ?? string.Empty, "Images", p.ImagePath));
        }

        imageCandidates.Add("Icon.jpg");
        imageCandidates.Add(Path.Combine("Images", "Icon.jpg"));
        imageCandidates.Add(Path.Combine(AppContext.BaseDirectory ?? string.Empty, "Icon.jpg"));
        imageCandidates.Add(Path.Combine(AppContext.BaseDirectory ?? string.Empty, "Images", "Icon.jpg"));

        foreach (var path in imageCandidates.Distinct())
        {
            if (!File.Exists(path)) continue;
            try
            {
                bmp = new Bitmap(path);
                break;
            }
            catch
            {
                // ignore image errors
            }
        }

        Control imageControl;
        if (bmp is not null)
        {
            imageControl = new Image
            {
                Source = bmp,
                Width = 160,
                Height = 160,
                Stretch = Avalonia.Media.Stretch.Uniform,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
        }
        else
        {
            imageControl = new Border
            {
                Width = 160,
                Height = 160,
                Background = Brushes.LightGray,
                CornerRadius = new CornerRadius(8),
                Child = new TextBlock
                {
                    Text = "Нет фото",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Foreground = Brushes.Gray
                },
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
        }

        rootGrid.Children.Add(imageControl);
        Grid.SetColumn(imageControl, 0);

        // Центр: информация
        var infoPanel = new StackPanel
        {
            Spacing = 4,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        infoPanel.Children.Add(new TextBlock
        {
            Text = $"{p.Name} ({p.Category})",
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.Bold
        });
        infoPanel.Children.Add(new TextBlock { Text = $"Артикул: {p.Article}" });
        if (!string.IsNullOrWhiteSpace(p.Description))
            infoPanel.Children.Add(new TextBlock { Text = p.Description, TextWrapping = TextWrapping.Wrap });
        infoPanel.Children.Add(new TextBlock { Text = $"Производитель: {p.Manufacturer}" });
        infoPanel.Children.Add(new TextBlock { Text = $"Поставщик: {p.Supplier}" });
        
        // Отображение цены: если есть скидка - показываем перечеркнутую цену красным и итоговую черным
        if (p.Discount > 0)
        {
            var finalPrice = p.Price * (1 - p.Discount / 100m);
            var pricePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            
            var originalPrice = new TextBlock
            {
                Text = $"{p.Price:0.##} ₽",
                TextDecorations = TextDecorations.Strikethrough,
                Foreground = Brushes.Red
            };
            
            var finalPriceText = new TextBlock
            {
                Text = $"{finalPrice:0.##} ₽ за {p.Unit}",
                Foreground = Brushes.Black
            };
            
            pricePanel.Children.Add(originalPrice);
            pricePanel.Children.Add(finalPriceText);
            infoPanel.Children.Add(pricePanel);
        }
        else
        {
            infoPanel.Children.Add(new TextBlock { Text = $"Цена: {p.Price:0.##} ₽ за {p.Unit}" });
        }
        
        infoPanel.Children.Add(new TextBlock { Text = $"Количество на складе: {p.Quantity}" });

        rootGrid.Children.Add(infoPanel);
        Grid.SetColumn(infoPanel, 1);

        // Правая часть: скидка
        var discountPanel = new StackPanel
        {
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        discountPanel.Children.Add(new TextBlock
        {
            Text = "Скидка:",
            FontWeight = Avalonia.Media.FontWeight.Bold
        });
        discountPanel.Children.Add(new TextBlock
        {
            Text = $"{p.Discount} %",
            FontSize = 14
        });

        rootGrid.Children.Add(discountPanel);
        Grid.SetColumn(discountPanel, 2);

        return new Border
        {
            Margin = new Thickness(0, 0, 0, 12),
            Padding = new Thickness(10),
            BorderBrush = Avalonia.Media.Brushes.Gray,
            BorderThickness = new Thickness(1),
            Child = rootGrid,
            Background = p.Quantity == 0 ? Avalonia.Media.Brushes.LightBlue : Avalonia.Media.Brushes.Transparent
        };
    }

    private void BackButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var main = new MainWindow();
        main.Show();
        Close();
    }
}
