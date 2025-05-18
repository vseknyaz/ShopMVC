using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopDomain.Model;
using ShopInfrastructure;
using System.Security.Claims;

namespace ShopInfrastructure.Controllers
{
    public class OrdersController : Controller
    {
        private readonly DbsportsContext _context;

        public OrdersController(DbsportsContext context)
        {
            _context = context;
        }

        // Перегляд усіх оформлених замовлень користувача
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Користувач не авторизований.");

            var orders = await _context.Orders
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.ProductSize)
                .ThenInclude(ps => ps.Product)
                .ThenInclude(p => p.Category)
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.ProductSize)
                .ThenInclude(ps => ps.Size)
                .Where(o => o.UserId == userId && o.StatusId == 2 && !o.IsDeleted)
                .ToListAsync();

            return View(orders);
        }

        // Деталі конкретного замовлення
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Користувач не авторизований.");

            var order = await _context.Orders
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.ProductSize)
                .ThenInclude(ps => ps.Product)
                .ThenInclude(p => p.Category)
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.ProductSize)
                .ThenInclude(ps => ps.Size)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId && !o.IsDeleted);

            if (order == null)
                return NotFound();

            return View(order);
        }
    }
}