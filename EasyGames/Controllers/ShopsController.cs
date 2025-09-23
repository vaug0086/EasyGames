using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

// this file is all pretty standard CRUD controller stuff - just for shops

namespace EasyGames.Controllers
{
    [Authorize(Roles = "Admin")] // only admins can alter CRUD shops
    public class ShopsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShopsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Shops
        public async Task<IActionResult> Index()
        {
            var shops = await _context.Shops
            // we also want the proprietor and their stock
                .Include(s => s.Proprietor)
                .Include(s => s.ShopStock)
                .ToListAsync();
            return View(shops);
        }

        // GET: Shops/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shop = await _context.Shops
                .Include(s => s.Proprietor)
                .Include(s => s.ShopStock)
                    .ThenInclude(ss => ss.StockItem) // this is nested so we can get the stock item models from the related shop stock model
                                                     // https://entityframework-classic.net/then-include
                .FirstOrDefaultAsync(m => m.ShopId == id);

            if (shop == null)
            {
                return NotFound();
            }

            return View(shop);
        }

        // GET: Shops/Create
        public IActionResult Create()
        {
            ViewData["ProprietorUserId"] = new SelectList(_context.Users, "Id", "Email"); // pass all the possible proprietors to view if they admin wants to create new shop
            return View();
        }

        // POST: Shops/Create
        // this is actually creating a new shop
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Address,ProprietorUserId")] Shop shop)
        {
            if (ModelState.IsValid)
            {
                _context.Add(shop);
                await _context.SaveChangesAsync();
                TempData["AlertSuccess"] = $"Shop '{shop.Name}' has been created successfully.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProprietorUserId"] = new SelectList(_context.Users, "Id", "Email", shop.ProprietorUserId);
            return View(shop);
        }

        // GET: Shops/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shop = await _context.Shops.FindAsync(id);
            if (shop == null)
            {
                return NotFound();
            }
            ViewData["ProprietorUserId"] = new SelectList(_context.Users, "Id", "Email", shop.ProprietorUserId);
            return View(shop);
        }

        // POST: Shops/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ShopId,Name,Address,ProprietorUserId")] Shop shop)
        {
            if (id != shop.ShopId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(shop);
                    await _context.SaveChangesAsync();
                    TempData["AlertSuccess"] = $"Shop '{shop.Name}' has been updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShopExists(shop.ShopId))
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
            ViewData["ProprietorUserId"] = new SelectList(_context.Users, "Id", "Email", shop.ProprietorUserId);
            return View(shop);
        }

        // GET: Shops/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shop = await _context.Shops
                .Include(s => s.Proprietor)
                .FirstOrDefaultAsync(m => m.ShopId == id);
            if (shop == null)
            {
                return NotFound();
            }

            return View(shop);
        }

        // POST: Shops/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var shop = await _context.Shops.FindAsync(id);
            if (shop != null)
            {
                _context.Shops.Remove(shop);
                await _context.SaveChangesAsync();
                TempData["AlertSuccess"] = $"Shop '{shop.Name}' has been deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ShopExists(int id)
        {
            return _context.Shops.Any(e => e.ShopId == id);
        }
    }
}