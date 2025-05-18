using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopDomain.Model;
using ShopInfrastructure;
using System.Security.Claims;

namespace ShopInfrastructure.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly DbsportsContext _context;

        public CartController(DbsportsContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productSizeId, int quantity)
        {
            if (quantity <= 0) return BadRequest("Кількість має бути позитивною.");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Користувач не авторизований.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cart = await _context.Orders
                    .FirstOrDefaultAsync(o => o.UserId == userId && o.StatusId == 1 && !o.IsDeleted);

                if (cart == null)
                {
                    cart = new Order
                    {
                        UserId = userId,
                        StatusId = 1,
                        OrderDate = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    _context.Orders.Add(cart);
                    await _context.SaveChangesAsync();
                }

                var productSize = await _context.ProductSizes
                    .FirstOrDefaultAsync(ps => ps.Id == productSizeId);
                if (productSize == null)
                    return BadRequest("Товар недоступний.");
                if (productSize.StockQuantity < quantity)
                    return BadRequest("Недостатньо товару на складі.");

                var cartItem = await _context.OrderProducts
                    .FirstOrDefaultAsync(op => op.OrderId == cart.Id && op.ProductSizeId == productSizeId);

                if (cartItem == null)
                {
                    cartItem = new OrderProduct
                    {
                        OrderId = cart.Id,
                        ProductSizeId = productSizeId,
                        Quantity = quantity
                    };
                    _context.OrderProducts.Add(cartItem);
                }
                else
                {
                    cartItem.Quantity += quantity;
                    if (cartItem.Quantity > productSize.StockQuantity)
                        return BadRequest("Недостатньо товару на складі.");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Товар додано до кошика!";
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Помилка при додаванні товару до кошика.";
                return StatusCode(500, "Помилка при додаванні товару до кошика.");
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Користувач не авторизований.");

            var cart = await _context.Orders
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.ProductSize)
                .ThenInclude(ps => ps.Product)
                .ThenInclude(p => p.Category)
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.ProductSize)
                .ThenInclude(ps => ps.Size)
                .FirstOrDefaultAsync(o => o.UserId == userId && o.StatusId == 1 && !o.IsDeleted);

            return View(cart ?? new Order { OrderProducts = new List<OrderProduct>() });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            if (quantity < 0) return BadRequest("Кількість не може бути від’ємною.");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Користувач не авторизований.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cartItem = await _context.OrderProducts.FindAsync(id);
                if (cartItem == null) return NotFound();

                var cart = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == cartItem.OrderId && o.UserId == userId && o.StatusId == 1 && !o.IsDeleted);
                if (cart == null) return Unauthorized("Недостатньо прав для оновлення.");

                var productSize = await _context.ProductSizes.FindAsync(cartItem.ProductSizeId);
                if (productSize == null)
                    return BadRequest("Розмір товару не знайдено.");

                if (quantity == 0)
                {
                    _context.OrderProducts.Remove(cartItem);
                    TempData["SuccessMessage"] = "Товар видалено з кошика.";
                }
                else if (quantity <= productSize.StockQuantity)
                {
                    cartItem.Quantity = quantity;
                    TempData["SuccessMessage"] = "Кількість оновлено.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Недостатньо товару на складі.";
                    return BadRequest("Недостатньо товару на складі.");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Помилка при оновленні кількості.";
                return StatusCode(500, "Помилка при оновленні кількості.");
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Користувач не авторизований.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cartItem = await _context.OrderProducts.FindAsync(id);
                if (cartItem == null) return NotFound();

                var cart = await _context.Orders
                    .FirstOrDefaultAsync(o => o.UserId == userId && o.StatusId == 1 && !o.IsDeleted);
                if (cart == null)
                    return Unauthorized("Кошик не знайдено.");
                if (cartItem.OrderId != cart.Id)
                    return Unauthorized("Недостатньо прав для видалення.");

                _context.OrderProducts.Remove(cartItem);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Товар видалено з кошика.";
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Помилка при видаленні товару.";
                return StatusCode(500, "Помилка при видаленні товару.");
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Користувач не авторизований.");

            var cart = await _context.Orders
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.ProductSize)
                .FirstOrDefaultAsync(o => o.UserId == userId && o.StatusId == 1 && !o.IsDeleted);

            if (cart == null) return RedirectToAction("Index");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in cart.OrderProducts)
                {
                    var productSize = await _context.ProductSizes.FindAsync(item.ProductSizeId);
                    if (productSize == null)
                    {
                        TempData["ErrorMessage"] = "Розмір товару не знайдено.";
                        return BadRequest("Розмір товару не знайдено.");
                    }
                    if (productSize.StockQuantity < item.Quantity)
                    {
                        TempData["ErrorMessage"] = "Недостатньо товару на складі.";
                        return BadRequest("Недостатньо товару на складі.");
                    }
                    productSize.StockQuantity -= item.Quantity;
                }

                cart.StatusId = 2; // "Оформлено"
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Замовлення успішно оформлено!";
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Помилка при оформленні замовлення.";
                return StatusCode(500, "Помилка при оформленні замовлення.");
            }
            return RedirectToAction("Index", "Orders");
        }
    }
}