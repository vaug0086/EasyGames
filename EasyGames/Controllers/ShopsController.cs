using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;

// this file is all pretty standard CRUD controller stuff - just for shops

namespace EasyGames.Controllers
{
    [Authorize(Roles = "Admin")] // only admins can alter CRUD shops
    public class ShopsController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        //  we need to track users and roles. This means that proprietor role is assigned automatically when the user has Shops.
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private const string ProprietorRole = "Proprietor";

        public ShopsController(ApplicationDbContext context,
                               UserManager<ApplicationUser> userManager,
                               RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
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

                // Automatically assign Proprietor role to the new shop owner
                // can do this over and over - no effect if they are already a proprietor
                await SyncProprietorRoleAsync(shop.ProprietorUserId);

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
                    // Get the original shop to check if proprietor changed
                    var originalShop = await _context.Shops.AsNoTracking().FirstOrDefaultAsync(s => s.ShopId == id);
                    var oldProprietorId = originalShop?.ProprietorUserId;

                    _context.Update(shop);
                    await _context.SaveChangesAsync();

                    // role for the new proprietor
                    await SyncProprietorRoleAsync(shop.ProprietorUserId);

                    // if proprietor changed, also sync role for the old proprietor (they may have lost their last shop)
                    if (oldProprietorId != null && oldProprietorId != shop.ProprietorUserId)
                    {
                        await SyncProprietorRoleAsync(oldProprietorId);
                    }

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
                var proprietorId = shop.ProprietorUserId;

                _context.Shops.Remove(shop);
                await _context.SaveChangesAsync();

                // Remove Proprietor role if this was the user's last shop
                await SyncProprietorRoleAsync(proprietorId);

                TempData["AlertSuccess"] = $"Shop '{shop.Name}' has been deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ShopExists(int id)
        {
            return _context.Shops.Any(e => e.ShopId == id);
        }

        // manages the Proprietor role for a user based on their shop ownership.
        // adds the role if they own at least one shop, removes it if they own none.
        // NOTE FROM FRANK - this function written with the help of Claude Code 
        // PROMPT:
        // I need a function so the user should only 
        // be assigned proprietor role if they are 
        // assigned as the proprietor for 1 or more 
        // shops. If they are a new proprietor, this 
        // role should be assigned. If they are removed 
        // as a proprietor from their last shop (or the 
        // shop is deleted) then they should be removed 
        // as a proprietor role.
        // END PROMPT

        // Why is this function called SyncProprietorRole Async?
        // It syncs or "syncronizes" the proprietor role.
        // It does this asyncronously (hence, async)
        private async Task SyncProprietorRoleAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            // Ensure the Proprietor role exists
            if (!await _roleManager.RoleExistsAsync(ProprietorRole))
                await _roleManager.CreateAsync(new IdentityRole(ProprietorRole));

            // Check if user owns any shops
            var ownsShops = await _context.Shops.AnyAsync(s => s.ProprietorUserId == userId);
            var hasRole = await _userManager.IsInRoleAsync(user, ProprietorRole);

            // Add role if they own shops but don't have the role
            if (ownsShops && !hasRole)
            {
                await _userManager.AddToRoleAsync(user, ProprietorRole);
            }
            // Remove role if they don't own shops but have the role
            else if (!ownsShops && hasRole)
            {
                await _userManager.RemoveFromRoleAsync(user, ProprietorRole);
            }
        }
    }
}