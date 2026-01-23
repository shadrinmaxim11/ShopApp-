using Npgsql;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ShopApp.Services
{
    // ===================== MODELS =====================

    public class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Article { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? Manufacturer { get; set; }
        public string? Supplier { get; set; }
        public decimal Price { get; set; }
        public string? Unit { get; set; }
        public int Quantity { get; set; }
        public int Discount { get; set; }
        public string? ImagePath { get; set; }

        public override string ToString() => $"{Name} ({Article}) - {Price:0.##} ₽";
    }

    public class User
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public string? Login { get; set; }
        public string? Password { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
    }

    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public override string ToString() => Name;
    }

    public class OrderView
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string? UserLogin { get; set; }
        public string? Status { get; set; }
        public string? City { get; set; }
        public string? Street { get; set; }
        public string? House { get; set; }
        public decimal TotalSum { get; set; }
    }

    public class OrderFormData
    {
        public int UserId { get; set; }
        public int PointId { get; set; }
        public int StatusId { get; set; }
        public DateTime OrderDate { get; set; }
        public List<OrderItemData> Items { get; set; } = new();
    }

    public class OrderItemData
    {
        public int ProductId { get; set; }
        // Размер обуви
        public int Size { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderItemView
    {
        public int ItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Article { get; set; } = string.Empty;
        public decimal Price { get; set; }
        // Размер обуви
        public int Size { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }
    }

    public class ReportRow
    {
        public DateTime Date { get; set; }
        public int Orders { get; set; }
        public string PickupPoint { get; set; } = string.Empty;
        public decimal Sum { get; set; }
        
        // Свойства для форматированного отображения
        public string DateFormatted => Date.ToString("dd.MM.yyyy");
        public string SumFormatted => $"{Sum:0.00}";
    }

    public class SimpleUser
    {
        public int Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public override string ToString() => Login;
    }

    public class PickupPoint
    {
        public int Id { get; set; }
        public string Display { get; set; } = string.Empty;
        public override string ToString() => Display;
    }

    public class OrderStatus
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public override string ToString() => Name;
    }

    public class Manufacturer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public override string ToString() => Name;
    }

    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public override string ToString() => Name;
    }

    public class ProductType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public override string ToString() => Name;
    }

    // ===================== DB SERVICE =====================

    public class DbService
    {
        private readonly string connectionString =
            "Host=localhost;Username=postgres;Password=12345;Database=botinki";
        
        private static bool _migrationPerformed = false;
        private static readonly object _migrationLock = new object();

        // Небольшая автоматическая миграция схемы БД
        // Добавляем колонку size в order_items, если её ещё нет,
        // чтобы не было падения приложения при работе с размерами обуви.
        // Выполняется только один раз при первом использовании.
        public DbService()
        {
            if (_migrationPerformed) return;
            
            lock (_migrationLock)
            {
                if (_migrationPerformed) return;
                
                try
                {
                    using var connection = new NpgsqlConnection(connectionString);
                    connection.Open();
                    // IF NOT EXISTS поддерживается в современных версиях PostgreSQL
                    var sql = @"ALTER TABLE order_items
                                ADD COLUMN IF NOT EXISTS size integer NOT NULL DEFAULT 0;";
                    connection.Execute(sql);
                    _migrationPerformed = true;
                }
                catch
                {
                    // Игнорируем ошибку миграции, чтобы приложение всё равно работало,
                    // даже если нет прав или старая версия PostgreSQL.
                    // Устанавливаем флаг, чтобы не пытаться снова
                    _migrationPerformed = true;
                }
            }
        }

        public IEnumerable<Product> GetProducts()
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            var sql = @"
                SELECT
                    p.product_id     AS Id,
                    p.article        AS Article,
                    p.name           AS Name,
                    p.unit           AS Unit,
                    p.price          AS Price,
                    p.quantity       AS Quantity,
                    p.discount       AS Discount,
                    p.description    AS Description,
                    p.photo          AS ImagePath,
                    c.name           AS Category,
                    m.name           AS Manufacturer,
                    s.name           AS Supplier
                FROM products p
                LEFT JOIN categories c     ON p.category_id     = c.category_id
                LEFT JOIN manufacturers m  ON p.manufacturer_id = m.manufacturer_id
                LEFT JOIN suppliers s      ON p.supplier_id     = s.supplier_id
                ORDER BY p.product_id";

            return connection.Query<Product>(sql).ToList();
        }

        public int InsertProduct(Product p, int? categoryId, int? supplierId, int? manufacturerId, int? typeId = null)
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            var sql = @"
                INSERT INTO products
                (article, name, unit, price, supplier_id, manufacturer_id, category_id, discount, quantity, description, photo, type_id)
                VALUES (@Article, @Name, @Unit, @Price, @SupplierId, @ManufacturerId, @CategoryId, @Discount, @Quantity, @Description, @ImagePath, @TypeId)
                RETURNING product_id";
            return connection.ExecuteScalar<int>(sql, new
            {
                p.Article,
                p.Name,
                p.Unit,
                p.Price,
                SupplierId = supplierId,
                ManufacturerId = manufacturerId,
                CategoryId = categoryId,
                p.Discount,
                p.Quantity,
                p.Description,
                p.ImagePath,
                TypeId = typeId
            });
        }

        public void UpdateProduct(Product p, int? categoryId, int? supplierId, int? manufacturerId, int? typeId = null)
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            var sql = @"
                UPDATE products
                SET article = @Article,
                    name = @Name,
                    unit = @Unit,
                    price = @Price,
                    supplier_id = @SupplierId,
                    manufacturer_id = @ManufacturerId,
                    category_id = @CategoryId,
                    discount = @Discount,
                    quantity = @Quantity,
                    description = @Description,
                    photo = @ImagePath,
                    type_id = @TypeId
                WHERE product_id = @Id";
            connection.Execute(sql, new
            {
                p.Article,
                p.Name,
                p.Unit,
                p.Price,
                SupplierId = supplierId,
                ManufacturerId = manufacturerId,
                CategoryId = categoryId,
                p.Discount,
                p.Quantity,
                p.Description,
                p.ImagePath,
                TypeId = typeId,
                p.Id
            });
        }

        public void DeleteProduct(int id)
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            // Удаляем позиции заказа для продукта
            connection.Execute("DELETE FROM order_items WHERE product_id = @id", new { id });
            connection.Execute("DELETE FROM products WHERE product_id = @id", new { id });
        }

        public IEnumerable<OrderView> GetOrders()
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            var sql = @"
                SELECT
                    o.order_id   AS Id,
                    o.order_date AS OrderDate,
                    u.login      AS UserLogin,
                    os.name      AS Status,
                    pp.city      AS City,
                    pp.street    AS Street,
                    pp.house     AS House,
                    (
                        SELECT COALESCE(
                            SUM(oi.quantity * (p.price * (1 - p.discount / 100.0))),
                            0
                        )
                        FROM order_items oi
                        JOIN products p ON p.product_id = oi.product_id
                        WHERE oi.order_id = o.order_id
                    ) AS TotalSum
                FROM orders o
                JOIN users u ON u.user_id = o.user_id
                JOIN order_statuses os ON os.status_id = o.status_id
                JOIN pickup_points pp ON pp.point_id = o.point_id
                ORDER BY o.order_date DESC, o.order_id DESC";

            return connection.Query<OrderView>(sql).ToList();
        }

        public int InsertOrder(OrderFormData data)
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var sql = @"
                    INSERT INTO orders (user_id, point_id, order_date, status_id)
                    VALUES (@UserId, @PointId, @OrderDate, @StatusId)
                    RETURNING order_id";
                var orderId = connection.ExecuteScalar<int>(sql, new
                {
                    data.UserId,
                    data.PointId,
                    data.OrderDate,
                    data.StatusId
                }, transaction);

                // Добавляем позиции заказа (с указанием размера)
                foreach (var item in data.Items)
                {
                    connection.Execute(
                        "INSERT INTO order_items (order_id, product_id, size, quantity) VALUES (@OrderId, @ProductId, @Size, @Quantity)",
                        new { OrderId = orderId, item.ProductId, item.Size, item.Quantity },
                        transaction);
                }

                transaction.Commit();
                return orderId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void UpdateOrder(int id, OrderFormData data)
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var sql = @"
                    UPDATE orders
                    SET user_id = @UserId,
                        point_id = @PointId,
                        order_date = @OrderDate,
                        status_id = @StatusId
                    WHERE order_id = @Id";
                connection.Execute(sql, new
                {
                    data.UserId,
                    data.PointId,
                    data.OrderDate,
                    data.StatusId,
                    Id = id
                }, transaction);

                // Удаляем старые позиции
                connection.Execute("DELETE FROM order_items WHERE order_id = @Id", new { Id = id }, transaction);

                // Добавляем новые позиции (с указанием размера)
                foreach (var item in data.Items)
                {
                    connection.Execute(
                        "INSERT INTO order_items (order_id, product_id, size, quantity) VALUES (@OrderId, @ProductId, @Size, @Quantity)",
                        new { OrderId = id, item.ProductId, item.Size, item.Quantity },
                        transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public IEnumerable<OrderItemView> GetOrderItems(int orderId)
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            var sql = @"
                SELECT
                    oi.item_id AS ItemId,
                    oi.product_id AS ProductId,
                    p.name AS ProductName,
                    p.article AS Article,
                    p.price AS Price,
                    oi.size AS Size,
                    oi.quantity AS Quantity,
                    (oi.quantity * p.price * (1 - p.discount / 100.0)) AS Total
                FROM order_items oi
                JOIN products p ON p.product_id = oi.product_id
                WHERE oi.order_id = @OrderId
                ORDER BY oi.item_id";
            return connection.Query<OrderItemView>(sql, new { OrderId = orderId }).ToList();
        }

        public void DeleteOrder(int id)
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            connection.Execute("DELETE FROM order_items WHERE order_id = @id", new { id });
            connection.Execute("DELETE FROM orders WHERE order_id = @id", new { id });
        }

        public IEnumerable<ReportRow> GetOrderReport(DateTime? from, DateTime? to)
        {
            // Возвращаем каждый заказ отдельно с пунктом выдачи
            var orders = GetOrders();

            if (from.HasValue)
                orders = orders.Where(o => o.OrderDate.Date >= from.Value.Date);
            if (to.HasValue)
                orders = orders.Where(o => o.OrderDate.Date <= to.Value.Date);

            return orders
                .Select(o => new ReportRow
                {
                    Date = o.OrderDate,
                    Orders = 1, // Каждая строка - один заказ
                    PickupPoint = string.IsNullOrWhiteSpace(o.City) 
                        ? (string.IsNullOrWhiteSpace(o.Street) ? "Не указан" : $"{o.Street} {o.House ?? ""}")
                        : $"{o.City}, {o.Street ?? ""} {o.House ?? ""}".Trim(),
                    Sum = o.TotalSum
                })
                .OrderBy(r => r.Date)
                .ToList();
        }

        public User? AuthenticateUser(string login, string password)
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            var sql = @"
                SELECT
                    user_id     AS Id,
                    role_id     AS RoleId,
                    login       AS Login,
                    password    AS Password,
                    first_name  AS FirstName,
                    last_name   AS LastName,
                    middle_name AS MiddleName
                FROM users
                WHERE login = @login AND password = @password
                LIMIT 1";

            return connection.QueryFirstOrDefault<User>(sql, new { login, password });
        }

        public IEnumerable<Category> GetCategories()
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            var sql = @"
                SELECT
                    category_id AS Id,
                    name
                FROM categories
                ORDER BY category_id";

            return connection.Query<Category>(sql).ToList();
        }

        public IEnumerable<SimpleUser> GetUsers()
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            var sql = "SELECT user_id AS Id, login AS Login FROM users ORDER BY login";
            return connection.Query<SimpleUser>(sql).ToList();
        }

        public IEnumerable<OrderStatus> GetOrderStatuses()
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            var sql = "SELECT status_id AS Id, name FROM order_statuses ORDER BY status_id";
            return connection.Query<OrderStatus>(sql).ToList();
        }

        public IEnumerable<Manufacturer> GetManufacturers()
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            var sql = "SELECT manufacturer_id AS Id, name FROM manufacturers ORDER BY manufacturer_id";
            return connection.Query<Manufacturer>(sql).ToList();
        }

        public IEnumerable<Supplier> GetSuppliers()
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            var sql = "SELECT supplier_id AS Id, name FROM suppliers ORDER BY supplier_id";
            return connection.Query<Supplier>(sql).ToList();
        }

        public IEnumerable<ProductType> GetProductTypes()
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            var sql = "SELECT type_id AS Id, name FROM product_types ORDER BY type_id";
            return connection.Query<ProductType>(sql).ToList();
        }

        public IEnumerable<PickupPoint> GetPickupPoints()
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            var sql = @"
                SELECT point_id AS Id,
                       (city || ', ' || street || ', ' || house) AS Display
                FROM pickup_points
                ORDER BY point_id";
            return connection.Query<PickupPoint>(sql).ToList();
        }
    }
}
