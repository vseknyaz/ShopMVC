using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopDomain.Model;
using ShopInfrastructure;

namespace ShopInfrastructure.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly DbsportsContext _context;

        public ProductsController(DbsportsContext context)
        {
            _context = context;
        }

        private string GetCategoryName(int? categoryId)
        {
            if (!categoryId.HasValue) return null;
            return _context.Categories.FirstOrDefault(c => c.Id == categoryId)?.Name;
        }

        // GET: Products
        public async Task<IActionResult> Index(int? categoryId, int? genderId, string searchString, string name, string sortOrder = "name_asc", int pageNumber = 1, int pageSize = 5)
        {
            if (categoryId == null)
                return RedirectToAction("Index", "Categories");

            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = name ?? GetCategoryName(categoryId);

            ViewBag.NameSortParm = sortOrder == "name_asc" ? "name_desc" : "name_asc";
            ViewBag.PriceSortParm = sortOrder == "price_asc" ? "price_desc" : "price_asc";

            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Gender)
                .Include(p => p.ProductSizes)
                .ThenInclude(ps => ps.Size)
                .AsQueryable();

            products = products.Where(p => p.CategoryId == categoryId);

            if (genderId.HasValue)
            {
                products = products.Where(p => p.GenderId == genderId);
                ViewBag.GenderId = genderId;
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString) || p.Description.Contains(searchString));
                ViewBag.SearchString = searchString;
            }

            switch (sortOrder)
            {
                case "name_desc":
                    products = products.OrderByDescending(p => p.Name);
                    break;
                case "price_asc":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                default: // name_asc
                    products = products.OrderBy(p => p.Name);
                    break;
            }

            int totalItems = await products.CountAsync();
            var pagedProducts = await products
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            ViewData["Genders"] = new SelectList(_context.Genders, "Id", "Name", genderId);

            return View(pagedProducts);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id, int? categoryId)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Gender)
                .Include(p => p.ProductSizes)
                .ThenInclude(ps => ps.Size)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
                return NotFound();

            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = GetCategoryName(categoryId);
            return View(product);
        }

        // GET: Products/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create(int? categoryId)
        {
            if (categoryId == null)
                return NotFound();

            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = GetCategoryName(categoryId);
            ViewData["GenderId"] = new SelectList(_context.Genders, "Id", "Name");
            ViewData["Sizes"] = new MultiSelectList(_context.Sizes, "Id", "Name");
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(int? categoryId, [Bind("Name,Description,Price,CategoryId,GenderId,IsDeleted,Id")] Product product, int[] selectedSizes, int[] stockQuantities)
        {
            if (categoryId == null)
                return NotFound();

            product.CategoryId = categoryId;

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Add(product);
                    await _context.SaveChangesAsync();

                    if (selectedSizes != null && stockQuantities != null && selectedSizes.Length == stockQuantities.Length)
                    {
                        for (int i = 0; i < selectedSizes.Length; i++)
                        {
                            if (stockQuantities[i] < 0) continue;
                            _context.ProductSizes.Add(new ProductSize
                            {
                                ProductId = product.Id,
                                SizeId = selectedSizes[i],
                                StockQuantity = stockQuantities[i]
                            });
                        }
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = "Товар успішно створено!";
                    return RedirectToAction("Index", "Products", new { categoryId = product.CategoryId, name = GetCategoryName(categoryId) });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Помилка при створенні товару.");
                }
            }

            ViewData["GenderId"] = new SelectList(_context.Genders, "Id", "Name", product.GenderId);
            ViewData["Sizes"] = new MultiSelectList(_context.Sizes, "Id", "Name", selectedSizes);
            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = GetCategoryName(categoryId);
            return View(product);
        }

        // GET: Products/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id, int? categoryId)
        {
            if (id == null || categoryId == null)
                return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            var selectedSizes = await _context.ProductSizes
                .Where(ps => ps.ProductId == id)
                .Select(ps => ps.SizeId)
                .ToListAsync();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["GenderId"] = new SelectList(_context.Genders, "Id", "Name", product.GenderId);
            ViewData["Sizes"] = new MultiSelectList(_context.Sizes, "Id", "Name", selectedSizes);
            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = GetCategoryName(categoryId);
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, int? categoryId, [Bind("Name,Description,Price,CategoryId,GenderId,IsDeleted,Id")] Product product, int[] selectedSizes, int[] stockQuantities)
        {
            if (id != product.Id || categoryId == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    var existingSizes = _context.ProductSizes.Where(ps => ps.ProductId == id);
                    _context.ProductSizes.RemoveRange(existingSizes);

                    if (selectedSizes != null && stockQuantities != null && selectedSizes.Length == stockQuantities.Length)
                    {
                        for (int i = 0; i < selectedSizes.Length; i++)
                        {
                            if (stockQuantities[i] < 0) continue;
                            _context.ProductSizes.Add(new ProductSize
                            {
                                ProductId = id,
                                SizeId = selectedSizes[i],
                                StockQuantity = stockQuantities[i]
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = "Товар успішно оновлено!";
                    return RedirectToAction("Index", new { categoryId, name = GetCategoryName(categoryId) });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                        return NotFound();
                    throw;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Помилка при редагуванні товару.");
                }
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["GenderId"] = new SelectList(_context.Genders, "Id", "Name", product.GenderId);
            ViewData["Sizes"] = new MultiSelectList(_context.Sizes, "Id", "Name", selectedSizes);
            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = GetCategoryName(categoryId);
            return View(product);
        }

        // GET: Products/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id, int? categoryId)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Gender)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
                return NotFound();

            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = GetCategoryName(categoryId);
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id, int? categoryId)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Товар успішно видалено!";
                return RedirectToAction(nameof(Index), new { categoryId, name = GetCategoryName(categoryId) });
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Помилка при видаленні товару.";
                return RedirectToAction(nameof(Index), new { categoryId, name = GetCategoryName(categoryId) });
            }
        }

        // POST: Products/AddToCart
        [HttpPost]
        public IActionResult AddToCart(int productSizeId, int quantity)
        {
            if (quantity <= 0)
                return BadRequest("Кількість має бути позитивною.");
            return RedirectToAction("AddToCart", "Cart", new { productSizeId, quantity });
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}