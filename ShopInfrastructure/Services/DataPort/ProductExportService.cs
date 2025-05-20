// File: ShopInfrastructure/Services/DataPort/ProductExportService.cs
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ShopDomain.Model;

namespace ShopInfrastructure.Services.DataPort
{
    public class ProductExportService : IExportService<Product>
    {
        private readonly DbsportsContext _context;
        public ProductExportService(DbsportsContext context) => _context = context;

        public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            using var wb = new XLWorkbook();
            var cats = await _context.Categories
                .Include(c => c.Products).ThenInclude(p => p.Gender)
                .Include(c => c.Products).ThenInclude(p => p.ProductSizes).ThenInclude(ps => ps.Size)
                .ToListAsync(cancellationToken);

            int maxSizes = cats.SelectMany(c => c.Products)
                               .Max(p => p.ProductSizes.Count);

            foreach (var cat in cats)
            {
                var ws = wb.Worksheets.Add(cat.Name);
                // Header
                var headers = new[] { "Назва", "Опис", "Ціна", "Стать" };
                for (int i = 0; i < headers.Length; i++)
                    ws.Cell(1, i + 1).Value = headers[i];
                for (int j = 0; j < maxSizes; j++)
                {
                    ws.Cell(1, 5 + j * 2).Value = $"Розмір {j + 1}";
                    ws.Cell(1, 6 + j * 2).Value = $"Кількість {j + 1}";
                }
                ws.Row(1).Style.Font.Bold = true;

                // Data
                int row = 2;
                foreach (var p in cat.Products)
                {
                    ws.Cell(row, 1).Value = p.Name;
                    ws.Cell(row, 2).Value = p.Description;
                    ws.Cell(row, 3).Value = p.Price;
                    ws.Cell(row, 4).Value = p.Gender?.Name;
                    int col = 5;
                    foreach (var ps in p.ProductSizes)
                    {
                        ws.Cell(row, col++).Value = ps.Size.Name;
                        ws.Cell(row, col++).Value = ps.StockQuantity;
                    }
                    row++;
                }
            }

            wb.SaveAs(stream);
        }
    }
}
