using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopDomain.Model;
using ShopInfrastructure;

namespace ShopInfrastructure.Controllers
{
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
        public async Task<IActionResult> Index(int? categoryId, int? genderId, string searchString, string? name, string sortOrder = "name_asc", int pageNumber = 1, int pageSize = 5)
        {
            if (categoryId == null)
                return RedirectToAction("Index", "Categories");

            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = name;

            ViewBag.NameSortParm = sortOrder == "name_asc" ? "name_desc" : "name_asc";
            ViewBag.PriceSortParm = sortOrder == "price_asc" ? "price_desc" : "price_asc";

            var products = _context.Products
                .Include(b => b.Category)
                .Include(p => p.Gender)
                .Include(p => p.ProductSizes) // Додаємо ProductSizes
                .ThenInclude(ps => ps.Size)   // Додаємо пов’язані розміри
                .AsQueryable();

            // Фільтрація за категорією
            products = products.Where(p => p.CategoryId == categoryId);

            // Фільтрація за статтю
            if (genderId.HasValue)
            {
                products = products.Where(p => p.GenderId == genderId);
                ViewBag.GenderId = genderId;
            }

            // Пошук
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString) || p.Description.Contains(searchString));
                ViewBag.SearchString = searchString;
            }

            // Сортування
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

            // Пагінація
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
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Gender)
                .Include(p => p.ProductSizes)
                .ThenInclude(ps => ps.Size)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = GetCategoryName(categoryId);
            return View(product);
        }

        // GET: Products/Create
        // GET: Products/Create
        public IActionResult Create(int? categoryId)
        {
            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = _context.Categories.FirstOrDefault(c => c.Id == categoryId)?.Name;
            ViewData["GenderId"] = new SelectList(_context.Genders, "Id", "Name");
            ViewData["Sizes"] = new MultiSelectList(_context.Sizes, "Id", "Name"); // Список розмірів
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int? categoryId, [Bind("Name,Description,Price,Stock,CategoryId,GenderId,IsDeleted,Id")] Product product, int[] selectedSizes)
        {
            product.CategoryId = categoryId;

            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();

                // Додаємо зв’язки з розмірами
                if (selectedSizes != null)
                {
                    foreach (var sizeId in selectedSizes)
                    {
                        _context.ProductSizes.Add(new ProductSize { ProductId = product.Id, SizeId = sizeId });
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("Index", "Products", new { categoryId = product.CategoryId, name = _context.Categories.FirstOrDefault(c => c.Id == categoryId)?.Name });
            }

            ViewData["GenderId"] = new SelectList(_context.Genders, "Id", "Name", product.GenderId);
            ViewData["Sizes"] = new MultiSelectList(_context.Sizes, "Id", "Name", selectedSizes);
            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = _context.Categories.FirstOrDefault(c => c.Id == categoryId)?.Name;
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id, int? categoryId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var selectedSizes = await _context.ProductSizes
                .Where(ps => ps.ProductId == id)
                .Select(ps => ps.SizeId)
                .ToListAsync();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["GenderId"] = new SelectList(_context.Genders, "Id", "Name", product.GenderId);
            ViewData["Sizes"] = new MultiSelectList(_context.Sizes, "Id", "Name", selectedSizes);
            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = _context.Categories.FirstOrDefault(c => c.Id == categoryId)?.Name;
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int? categoryId, [Bind("Name,Description,Price,Stock,CategoryId,GenderId,IsDeleted,Id")] Product product, int[] selectedSizes)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    // Оновлюємо зв’язки з розмірами
                    var existingSizes = _context.ProductSizes.Where(ps => ps.ProductId == id);
                    _context.ProductSizes.RemoveRange(existingSizes);

                    if (selectedSizes != null)
                    {
                        foreach (var sizeId in selectedSizes)
                        {
                            _context.ProductSizes.Add(new ProductSize { ProductId = id, SizeId = sizeId });
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index", new { categoryId, name = _context.Categories.FirstOrDefault(c => c.Id == categoryId)?.Name });
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["GenderId"] = new SelectList(_context.Genders, "Id", "Name", product.GenderId);
            ViewData["Sizes"] = new MultiSelectList(_context.Sizes, "Id", "Name", selectedSizes);
            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = _context.Categories.FirstOrDefault(c => c.Id == categoryId)?.Name;
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Gender)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}