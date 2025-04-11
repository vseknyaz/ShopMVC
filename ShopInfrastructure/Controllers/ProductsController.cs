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

        // GET: Products
        public async Task<IActionResult> Index(int? id, string? name)
        {
            if(id == null) return RedirectToAction("Categories", "Index");
            // знахоження продуктів за категорією
            ViewBag.CategoryId = id;
            ViewBag.CategoryName = name;

            var productByCategory = _context.Products.Where(b => b.CategoryId == id).Include(b => b.Category);
            
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
            ViewBag.CategoryName = _context.Categories.FirstOrDefault(c => c.Id == categoryId)?.Name;
            //ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", categoryId);
            ViewData["GenderId"] = new SelectList(_context.Genders, "Id", "Name");
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int? categoryId,[Bind("Name,Description,Price,Stock,CategoryId,GenderId,IsDeleted,Id")] Product product)
        {
            product.CategoryId = categoryId;
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                //return RedirectToAction(nameof(Index));
                return RedirectToAction("Index", "Products", new { id = product.CategoryId, name = _context.Categories.Where(c => c.Id == categoryId).FirstOrDefault().Name });
            }
            //ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["GenderId"] = new SelectList(_context.Genders, "Id", "Name", product.GenderId);
            //return View(product);
            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = _context.Categories.FirstOrDefault(c => c.Id == categoryId)?.Name;
            return RedirectToAction("Index", "Products", new { id = product.CategoryId, name = _context.Categories.Where(c => c.Id == categoryId).FirstOrDefault().Name });
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
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
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,Description,Price,Stock,CategoryId,GenderId,IsDeleted,Id")] Product product)
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
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["GenderId"] = new SelectList(_context.Genders, "Id", "Name", product.GenderId);
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
