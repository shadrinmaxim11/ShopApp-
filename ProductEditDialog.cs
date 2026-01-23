using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ShopApp.Services;

namespace ShopApp;

public class ProductFormData
{
    public int? ProductId { get; set; }
    public string Article { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "шт.";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int Discount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public int? ManufacturerId { get; set; }
    public int? TypeId { get; set; }

    public Product ToProduct() => new()
    {
        Article = Article,
        Name = Name,
        Unit = Unit,
        Price = Price,
        Quantity = Quantity,
        Discount = Discount,
        Description = Description,
        ImagePath = ImagePath
    };

    public Product ToProductWithId()
    {
        var p = ToProduct();
        p.Id = ProductId ?? 0;
        return p;
    }
}

public class ProductEditDialog : Window
{
    private readonly TextBox _article = new() { Watermark = "Артикул" };
    private readonly TextBox _name = new() { Watermark = "Наименование" };
    private readonly TextBox _unit = new() { Watermark = "Ед. изм." };
    private readonly TextBox _price = new() { Watermark = "Цена" };
    private readonly TextBox _qty = new() { Watermark = "Количество" };
    private readonly TextBox _discount = new() { Watermark = "Скидка %" };
    private readonly TextBox _description = new() { Watermark = "Описание", AcceptsReturn = true, Height = 80 };
    private readonly TextBox _photo = new() { Watermark = "Путь к фото (необязательно)" };
    private readonly ComboBox _category = new();
    private readonly ComboBox _supplier = new();
    private readonly ComboBox _manufacturer = new();
    private readonly ComboBox _type = new();

    private readonly DbService _db;

    public ProductEditDialog(DbService db, Product? existing)
    {
        _db = db;
        Title = existing == null ? "Добавить товар" : "Редактировать товар";
        Width = 480;
        Height = 640;

        var categories = _db.GetCategories().ToList();
        var suppliers = _db.GetSuppliers().ToList();
        var manufacturers = _db.GetManufacturers().ToList();
        var types = _db.GetProductTypes().ToList();

        _category.ItemsSource = categories;
        _supplier.ItemsSource = suppliers;
        _manufacturer.ItemsSource = manufacturers;
        _type.ItemsSource = types;

        if (existing != null)
        {
            _article.Text = existing.Article;
            _name.Text = existing.Name;
            _unit.Text = existing.Unit;
            _price.Text = existing.Price.ToString("0.##");
            _qty.Text = existing.Quantity.ToString();
            _discount.Text = existing.Discount.ToString();
            _description.Text = existing.Description;
            _photo.Text = existing.ImagePath;
            _category.SelectedItem = categories.FirstOrDefault(c => c.Name == existing.Category);
            _manufacturer.SelectedItem = manufacturers.FirstOrDefault(m => m.Name == existing.Manufacturer);
            _supplier.SelectedItem = suppliers.FirstOrDefault(s => s.Name == existing.Supplier);
        }
        else
        {
            _unit.Text = "шт.";
        }

        var okButton = new Button { Content = "Сохранить", Width = 100 };
        var cancelButton = new Button { Content = "Отмена", Width = 100 };
        okButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_article.Text) || string.IsNullOrWhiteSpace(_name.Text))
            {
                Close(null);
                return;
            }

            if (!decimal.TryParse(_price.Text, out var price))
                price = 0m;
            if (!int.TryParse(_qty.Text, out var qty))
                qty = 0;
            if (!int.TryParse(_discount.Text, out var discount))
                discount = 0;

            var data = new ProductFormData
            {
                ProductId = existing?.Id,
                Article = _article.Text.Trim(),
                Name = _name.Text.Trim(),
                Unit = _unit.Text?.Trim() ?? "шт.",
                Price = price,
                Quantity = qty,
                Discount = discount,
                Description = _description.Text ?? string.Empty,
                ImagePath = string.IsNullOrWhiteSpace(_photo.Text) ? null : _photo.Text.Trim(),
                CategoryId = (_category.SelectedItem as Category)?.Id,
                SupplierId = (_supplier.SelectedItem as Supplier)?.Id,
                ManufacturerId = (_manufacturer.SelectedItem as Manufacturer)?.Id,
                TypeId = (_type.SelectedItem as ProductType)?.Id
            };
            Close(data);
        };
        cancelButton.Click += (_, _) => Close(null);

        Content = new ScrollViewer
        {
            Content = new StackPanel
            {
                Margin = new Thickness(12),
                Spacing = 8,
                Children =
                {
                    new TextBlock{ Text="Артикул"},
                    _article,
                    new TextBlock{ Text="Наименование"},
                    _name,
                    new TextBlock{ Text="Ед. изм."},
                    _unit,
                    new TextBlock{ Text="Цена"},
                    _price,
                    new TextBlock{ Text="Количество"},
                    _qty,
                    new TextBlock{ Text="Скидка %"},
                    _discount,
                    new TextBlock{ Text="Категория"},
                    _category,
                    new TextBlock{ Text="Поставщик"},
                    _supplier,
                    new TextBlock{ Text="Производитель"},
                    _manufacturer,
                    new TextBlock{ Text="Тип (необязательно)"},
                    _type,
                    new TextBlock{ Text="Описание"},
                    _description,
                    new TextBlock{ Text="Фото (путь)"},
                    _photo,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { cancelButton, okButton }
                    }
                }
            }
        };
    }
}

