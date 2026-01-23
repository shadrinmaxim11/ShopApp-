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

public partial class AdminProductsWindow : Window
{
    private readonly DbService _db = new DbService();
    private readonly List<Product> _products = new();
    private Product? _selectedProduct;

    public AdminProductsWindow()
    {
        InitializeComponent();
        BackButton.Click += BackButton_Click;
        SearchButton.Click += SearchButton_Click;
        SortCombo.SelectionChanged += SortCombo_SelectionChanged;
        CategoryCombo.SelectionChanged += (_, _) => ApplyFiltersAndRender();

        AddProductButton.Click += AddProductButton_Click;
        EditProductButton.Click += EditProductButton_Click;
        DeleteProductButton.Click += DeleteProductButton_Click;

        Loaded += AdminProductsWindow_Loaded;
    }

    private async void AdminProductsWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        await LoadCategoriesAsync();
        await LoadProductsAsync();
        RenderProducts(_products);
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await Task.Run(() =>
        {
            return _db.GetCategories().Select(c => c.Name).Prepend("Все категории").ToList();
        });
        CategoryCombo.ItemsSource = categories;
        CategoryCombo.SelectedIndex = 0;
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
        var list = products.ToList();
        ProductsPanel.Children.Clear();
        
        // Сохраняем выделенный товар перед очисткой
        var selectedId = _selectedProduct?.Id;
        
        if (list.Count == 0)
        {
            ProductsPanel.Children.Add(new TextBlock { Text = "Товары не найдены." });
            _selectedProduct = null;
            return;
        }

        Border? selectedBorder = null;
        foreach (var p in list)
        {
            var card = CreateProductCard(p);
            ProductsPanel.Children.Add(card);
            
            // Восстанавливаем выделение
            if (selectedId.HasValue && p.Id == selectedId.Value && card is Border border)
            {
                border.BorderBrush = Brushes.Blue;
                border.BorderThickness = new Thickness(2);
                _selectedProduct = p;
                selectedBorder = border;
            }
        }
    }

    private Control CreateProductCard(Product p)
    {
        // Основная сетка карточки: слева картинка, в центре информация, справа надпись "Скидка"
        var rootGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("180,*,100"),
            ColumnSpacing = 10
        };

        // --------- Левая часть: изображение товара ---------
        Bitmap? bmp = null;
        var imageCandidates = new List<string>();

        // Если указан путь к фото в БД – пробуем его
        if (!string.IsNullOrWhiteSpace(p.ImagePath))
        {
            imageCandidates.Add(p.ImagePath);
            imageCandidates.Add(Path.Combine("Images", p.ImagePath));
            imageCandidates.Add(Path.Combine(AppContext.BaseDirectory ?? string.Empty, p.ImagePath));
            imageCandidates.Add(Path.Combine(AppContext.BaseDirectory ?? string.Empty, "Images", p.ImagePath));
        }

        // Если своё фото не найдено – используем Icon.jpg из разных возможных мест
        imageCandidates.Add("Icon.jpg");
        imageCandidates.Add(Path.Combine("Images", "Icon.jpg"));
        imageCandidates.Add(Path.Combine(AppContext.BaseDirectory ?? string.Empty, "Icon.jpg"));
        imageCandidates.Add(Path.Combine(AppContext.BaseDirectory ?? string.Empty, "Images", "Icon.jpg"));

        foreach (var path in imageCandidates.Distinct())
        {
            if (File.Exists(path))
            {
                try
                {
                    bmp = new Bitmap(path);
                    break;
                }
                catch
                {
                    // если файл битый – просто пробуем следующий
                }
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
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
        else
        {
            // На крайний случай простая заглушка
            imageControl = new Border
            {
                Width = 160,
                Height = 160,
                Background = Brushes.LightGray,
                CornerRadius = new CornerRadius(8),
                Child = new TextBlock
                {
                    Text = "Нет фото",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Foreground = Brushes.Gray
                },
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        rootGrid.Children.Add(imageControl);
        Grid.SetColumn(imageControl, 0);

        // --------- Центральная часть: основная информация ---------
        var infoPanel = new StackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };

        infoPanel.Children.Add(new TextBlock
        {
            Text = $"{p.Name} ({p.Category})",
            FontSize = 16,
            FontWeight = FontWeight.Bold
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
        
        infoPanel.Children.Add(new TextBlock 
        { 
            Text = $"Количество на складе: {p.Quantity}",
            Foreground = p.Quantity == 0 ? Brushes.Green : Brushes.Black
        });

        rootGrid.Children.Add(infoPanel);
        Grid.SetColumn(infoPanel, 1);

        // --------- Правая часть: надпись про скидку ---------
        var discountPanel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        discountPanel.Children.Add(new TextBlock
        {
            Text = "Скидка:",
            FontWeight = FontWeight.Bold
        });

        discountPanel.Children.Add(new TextBlock
        {
            Text = $"{p.Discount} %",
            FontSize = 14,
            Foreground = p.Quantity == 0 ? Brushes.Green : (p.Discount > 15 ? Brushes.Blue : Brushes.Black)
        });

        rootGrid.Children.Add(discountPanel);
        Grid.SetColumn(discountPanel, 2);

        var border = new Border
        {
            Margin = new Thickness(0, 0, 0, 12),
            Padding = new Thickness(10),
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Child = rootGrid,
            Tag = p,
            Background = p.Quantity == 0 ? Brushes.LightBlue : Brushes.Transparent
        };
        
        // Делаем карточку кликабельной для выбора
        border.PointerPressed += (s, e) =>
        {
            // Сбрасываем выделение у всех карточек
            foreach (var child in ProductsPanel.Children)
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
            _selectedProduct = p;
        };
        
        return border;
    }

    private void SearchButton_Click(object? sender, RoutedEventArgs e) => ApplyFiltersAndRender();
    private void SortCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e) => ApplyFiltersAndRender();

    private void ApplyFiltersAndRender()
    {
        var text = SearchBox.Text?.Trim() ?? string.Empty;
        var cat = (CategoryCombo.SelectedItem?.ToString()) ?? string.Empty;
        if (cat == "Все категории") cat = string.Empty;
        var filtered = _products.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(text))
        {
            text = text.ToLowerInvariant();
            filtered = filtered.Where(p =>
                (p.Name ?? string.Empty).ToLowerInvariant().Contains(text) ||
                (p.Article ?? string.Empty).ToLowerInvariant().Contains(text) ||
                (p.Description ?? string.Empty).ToLowerInvariant().Contains(text));
        }

        if (!string.IsNullOrWhiteSpace(cat))
        {
            var catLower = cat.ToLowerInvariant();
            filtered = filtered.Where(p => (p.Category ?? string.Empty).ToLowerInvariant().Contains(catLower));
        }

        var sortTag = (SortCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        filtered = sortTag switch
        {
            "price_asc" => filtered.OrderBy(p => p.Price),
            "price_desc" => filtered.OrderByDescending(p => p.Price),
            "discount_asc" => filtered.OrderBy(p => p.Discount),
            "discount_desc" => filtered.OrderByDescending(p => p.Discount),
            "qty_desc" => filtered.OrderByDescending(p => p.Quantity),
            _ => filtered
        };

        RenderProducts(filtered);
    }

    private void BackButton_Click(object? sender, RoutedEventArgs e)
    {
        var admin = new AdminWindow();
        admin.Show();
        Close();
    }

    private async void AddProductButton_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new ProductEditDialog(_db, null);
        var result = await dialog.ShowDialog<ProductFormData?>(this);
        if (result is null) return;
        var newId = _db.InsertProduct(result.ToProduct(), result.CategoryId, result.SupplierId, result.ManufacturerId, result.TypeId);
        await LoadProductsAsync();
        ApplyFiltersAndRender();
    }

    private async void EditProductButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedProduct == null)
        {
            await ShowMessageDialog("Ошибка", "Выберите товар для редактирования (кликните на карточку товара)");
            return;
        }

        var dialog = new ProductEditDialog(_db, _selectedProduct);
        var result = await dialog.ShowDialog<ProductFormData?>(this);
        if (result is null) return;
        result.ProductId = _selectedProduct.Id;
        _db.UpdateProduct(result.ToProductWithId(), result.CategoryId, result.SupplierId, result.ManufacturerId, result.TypeId);
        _selectedProduct = null;
        await LoadProductsAsync();
        ApplyFiltersAndRender();
    }

    private async void DeleteProductButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedProduct == null)
        {
            await ShowMessageDialog("Ошибка", "Выберите товар для удаления (кликните на карточку товара)");
            return;
        }
        
        _db.DeleteProduct(_selectedProduct.Id);
        _selectedProduct = null;
        await LoadProductsAsync();
        ApplyFiltersAndRender();
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
            HorizontalAlignment = HorizontalAlignment.Right,
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

