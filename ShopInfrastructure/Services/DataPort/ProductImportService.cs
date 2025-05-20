// File: ShopInfrastructure/Services/DataPort/ProductImportService.cs
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using ShopDomain.Model;
using Microsoft.EntityFrameworkCore;

namespace ShopInfrastructure.Services.DataPort
{
    public class ProductImportService : IImportService<Product>
    {
        private readonly DbsportsContext _context;
        public ProductImportService(DbsportsContext context) => _context = context;

        public async Task ImportFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            using var workbook = new XLWorkbook(stream);
            foreach (var ws in workbook.Worksheets)
            {
                // 1. Category
                var categoryName = ws.Name;
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name == categoryName, cancellationToken)
                    ?? new Category { Name = categoryName };
                if (_context.Entry(category).State == EntityState.Detached)
                    _context.Categories.Add(category);

                // 2. Rows (skip header)
                foreach (var row in ws.RowsUsed().Skip(1))
                    await ProcessRowAsync(row, category, cancellationToken);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task ProcessRowAsync(IXLRow row, Category category, CancellationToken ct)
        {
            // 1) Зчитуємо основні поля
            var name = row.Cell(1).GetString();
            var description = row.Cell(2).GetString();
            var price = row.Cell(3).GetValue<decimal>();
            var genderN = row.Cell(4).GetString();

            // 2) Gender
            var gender = await _context.Genders
                .FirstOrDefaultAsync(g => g.Name == genderN, ct)
                ?? new Gender { Name = genderN };
            if (_context.Entry(gender).State == EntityState.Detached)
                _context.Genders.Add(gender);

            // 3) Product (з Include розмірів)
            var product = await _context.Products
                .Include(p => p.ProductSizes)
                    .ThenInclude(ps => ps.Size)
                .FirstOrDefaultAsync(p => p.Name == name
                                        && p.CategoryId == category.Id, ct);

            if (product == null)
            {
                product = new Product
                {
                    Name = name,
                    Description = description,
                    Price = price,
                    Category = category,
                    Gender = gender
                };
                _context.Products.Add(product);

                // створювати нові всі розміри далі в циклі
            }
            else
            {
                product.Description = description;
                product.Price = price;
                product.Gender = gender;
                // не видаляємо все без розбору
            }

            // 4) Підготуємо словник існуючих ProductSize
            var existingSizes = product.ProductSizes
                .ToDictionary(ps => ps.Size.Name, ps => ps);

            var importedSizeNames = new HashSet<string>();
            // 5) Цикл по парах колонок (size, qty)
            for (int col = 5; ; col += 2)
            {
                var sizeName = row.Cell(col).GetString();
                if (string.IsNullOrWhiteSpace(sizeName)) break;
                var qty = row.Cell(col + 1).GetValue<int>();
                if (qty < 0) continue;

                importedSizeNames.Add(sizeName);

                if (existingSizes.TryGetValue(sizeName, out var existingPs))
                {
                    // оновлюємо
                    existingPs.StockQuantity = qty;
                }
                else
                {
                    // додаємо новий
                    var size = await _context.Sizes
                        .FirstOrDefaultAsync(s => s.Name == sizeName, ct)
                        ?? new Size { Name = sizeName };
                    if (_context.Entry(size).State == EntityState.Detached)
                        _context.Sizes.Add(size);

                    _context.ProductSizes.Add(new ProductSize
                    {
                        Product = product,
                        Size = size,
                        StockQuantity = qty
                    });
                }
            }

            // 6) Видаляємо ті, які не імпортували, але тільки якщо без замовлень
            foreach (var kv in existingSizes)
            {
                var sizeName = kv.Key;
                var ps = kv.Value;
                if (importedSizeNames.Contains(sizeName))
                    continue;

                var hasOrders = await _context.OrderProducts
                    .AnyAsync(op => op.ProductSizeId == ps.Id, ct);
                if (!hasOrders)
                    _context.ProductSizes.Remove(ps);
            }
        }
    }
}
