using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopDomain.Model;
using ShopInfrastructure;
using System.Security.Claims;

namespace ShopInfrastructure.Controllers
{
    public class CartController : Controller
    {
        private readonly DbsportsContext _context;

        public CartController(DbsportsContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> AddToCart(int productSizeId, int quantity)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Користувач не авторизований.");

            var cart = await _context.Orders
                .FirstOrDefaultAsync(o => o.UserId == userId && o.StatusId == 1 && !o.IsDeleted);

            if (cart == null)
            {
                cart = new Order
                {
                    UserId = userId,
                    StatusId = 1,
                    OrderDate = DateTime.Now,
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

        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            var cartItem = await _context.OrderProducts.FindAsync(id);
            if (cartItem == null) return NotFound();

            var productSize = await _context.ProductSizes.FindAsync(cartItem.ProductSizeId);
            if (productSize == null)
                return BadRequest("Розмір товару не знайдено.");

            if (quantity <= 0)
            {
                _context.OrderProducts.Remove(cartItem);
            }
            else if (quantity <= productSize.StockQuantity)
            {
                cartItem.Quantity = quantity;
            }
            else
            {
                return BadRequest("Недостатньо товару на складі.");
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Користувач не авторизований.");

            var cartItem = await _context.OrderProducts.FindAsync(id);
            if (cartItem == null) return NotFound();

            var cart = await _context.Orders
                .FirstOrDefaultAsync(o => o.UserId == userId && o.StatusId == 1 && !o.IsDeleted);
            if (cart == null || cartItem.OrderId != cart.Id)
                return NotFound();

            _context.OrderProducts.Remove(cartItem);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var cart = await _context.Orders
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.ProductSize)
                .FirstOrDefaultAsync(o => o.UserId == userId && o.StatusId == 1 && !o.IsDeleted);

            if (cart == null) return RedirectToAction("Index");

            foreach (var item in cart.OrderProducts)
            {
                var productSize = await _context.ProductSizes.FindAsync(item.ProductSizeId);
                if (productSize == null)
                    return BadRequest("Розмір товару не знайдено.");
                if (productSize.StockQuantity < item.Quantity)
                    return BadRequest("Недостатньо товару на складі.");
                productSize.StockQuantity -= item.Quantity;
            }

            cart.StatusId = 2; // "Оформлено"
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Orders");
        }
    }
}