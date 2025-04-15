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
        public async Task<IActionResult> Index(int? categoryId, string? name)
        {
            if (categoryId == null)
                return RedirectToAction("Index", "Categories"); // Виправлено перенаправлення

            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = name;

            var productByCategory = _context.Products
                .Where(b => b.CategoryId == categoryId)
                .Include(b => b.Category)
                .Include(p => p.Gender);

            return View(await productByCategory.ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
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

        // GET: Products/Create
        public IActionResult Create(int? categoryId)
        {
            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = GetCategoryName(categoryId);
            ViewData["GenderId"] = new SelectList(_context.Genders, "Id", "Name");
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int? categoryId, [Bind("Name,Description,Price,Stock,CategoryId,GenderId,IsDeleted,Id")] Product product)
        {
            product.CategoryId = categoryId;

            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Products", new { categoryId = product.CategoryId, name = GetCategoryName(categoryId) });
            }

            // Якщо валідація не пройшла, повертаємо форму з помилками
            ViewData["GenderId"] = new SelectList(_context.Genders, "Id", "Name", product.GenderId);
            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = GetCategoryName(categoryId);
            return View(product); // Виправлено: повертаємо форму, а не перенаправляємо
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

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["GenderId"] = new SelectList(_context.Genders, "Id", "Name", product.GenderId);
            ViewBag.CategoryId = categoryId; // Додаємо для "Back to List"
            ViewBag.CategoryName = GetCategoryName(categoryId); // Додаємо для "Back to List"
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int? categoryId, [Bind("Name,Description,Price,Stock,CategoryId,GenderId,IsDeleted,Id")] Product product)
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
                return RedirectToAction("Index", new { categoryId, name = GetCategoryName(categoryId) });
            }

            // Якщо валідація не пройшла, повертаємо форму з помилками
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["GenderId"] = new SelectList(_context.Genders, "Id", "Name", product.GenderId);
            ViewBag.CategoryId = categoryId; // Додаємо для "Back to List"
            ViewBag.CategoryName = GetCategoryName(categoryId); // Додаємо для "Back to List"
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