using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace EasyGames.Controllers
{
    [Authorize(Roles = "Proprietor")] // only proprietor can manage their own stock - not even the admin can do that
    public class ShopStockController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShopStockController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ShopStock
        public async Task<IActionResult> Index(int? shopId)
        {
            // this fetches the stock for the shop
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Gets user ID 
            var userShops = await _context.Shops
                .Where(s => s.ProprietorUserId == userId)
                .Include(s => s.ShopStock)
                    .ThenInclude(ss => ss.StockItem) // again, using ThenInclude to chain related objects
                .ToListAsync();

            if (!userShops.Any())
            {
                return NotFound();
            }

            Shop? selectedShop = null;
            if (shopId.HasValue)
            {
                selectedShop = userShops.FirstOrDefault(s => s.ShopId == shopId.Value); // only get first shop (should only be one anyway, this is just double sure)
            }
            else
            {
                selectedShop = userShops.First(); // standard case
            }

            if (selectedShop == null) // if no shop found, errors are caught
            {
                return NotFound();
            }

            ViewBag.UserShops = userShops; // we add this other information so the user can easily select a different shop to add stock to
            ViewBag.SelectedShopId = selectedShop.ShopId; // this just marks the currently selected shop - the user can change this in the view
            return View(selectedShop);
        }

        // GET: ShopStock/Transfer
        public async Task<IActionResult> Transfer(int? shopId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userShops = await _context.Shops
                .Where(s => s.ProprietorUserId == userId)
                .ToListAsync();

            if (!userShops.Any())
            {
                return NotFound();
            }

            Shop? selectedShop = null;
            if (shopId.HasValue)
            {
                selectedShop = userShops.FirstOrDefault(s => s.ShopId == shopId.Value);
            }
            else
            {
                selectedShop = userShops.First();
            }

            if (selectedShop == null)
            {
                return NotFound();
            }

            // Get available stock items (that have quantity > 0)
            var availableItems = await _context.StockItems
                .Where(si => si.Quantity > 0)
                .OrderBy(si => si.Category)
                .ThenBy(si => si.Name)
                .ToListAsync();
            
            // used ViewBag in my last assignemnt - but here's the reference again: https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-9.0#pass-data-to-views
            ViewBag.SelectedShop = selectedShop; // again, these two values make it possible for the user to add stock to a different store all on the one page
            ViewBag.UserShops = userShops;
            ViewBag.StockItemId = new SelectList(availableItems, "Id", "Name");

            return View();
        }

        // POST: ShopStock/Transfer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(int shopId, int stockItemId, int quantity, int lowStockThreshold = 5)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Verify shop belongs to current user
            var shop = await _context.Shops // this is an IMPORTANT check because we accept user input - the store they want to add stock to. We need to make sure this is actually their store, to minimise any security risks
                .FirstOrDefaultAsync(s => s.ShopId == shopId && s.ProprietorUserId == userId);

            if (shop == null)
            {
                return Unauthorized(); // catch any unauthorised access
            }

            // Get stock item
            var stockItem = await _context.StockItems.FindAsync(stockItemId);
            if (stockItem == null)
            {
                TempData["AlertDanger"] = "Stock item not found.";
                return RedirectToAction(nameof(Transfer), new { shopId });
            }

            // Check if main stock has enough quantity
            if (stockItem.Quantity < quantity)
            {
                TempData["AlertDanger"] = $"Insufficient stock. Only {stockItem.Quantity} items available.";
                return RedirectToAction(nameof(Transfer), new { shopId });
            }


            // ok we're using transactions - a bit more complex!
            // a transaction is a unit of work performed as a single op
            // if something fails in the transaction the whole transaction is cancelled. DB returns to its state before transaction started.
            // Why are we using a transaction here???
            // We need multiple operations here. Imagine if everything executes - but stock isn't updated... this would lead to inconsistencies!
            // Hence, transactions prevent the DB from becoming inconsistent
            // https://learn.microsoft.com/en-us/ef/core/saving/transactions
            // a transaction 
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check if shop already has this item
                var existingShopStock = await _context.ShopStock
                    .FirstOrDefaultAsync(ss => ss.ShopId == shopId && ss.StockItemId == stockItemId);

                if (existingShopStock != null)
                {
                    // Update existing shop stock
                    existingShopStock.QtyOnHand += quantity;
                    existingShopStock.LowStockThreshold = lowStockThreshold;
                }
                else
                {
                    // Create new shop stock entry
                    var newShopStock = new ShopStock
                    {
                        ShopId = shopId,
                        StockItemId = stockItemId,
                        QtyOnHand = quantity,
                        LowStockThreshold = lowStockThreshold,
                        InheritedBuyPrice = stockItem.BuyPrice,
                        InheritedSellPrice = stockItem.SellPrice
                    };
                    _context.ShopStock.Add(newShopStock);
                }

                // Reduce main stock quantity (permanent transfer)
                stockItem.Quantity -= quantity;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["AlertSuccess"] = $"Successfully transferred {quantity} units of '{stockItem.Name}' to shop.";
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["AlertDanger"] = "An error occurred during the transfer. Please try again.";
            }

            return RedirectToAction(nameof(Index), new { shopId });
        }

        // GET: ShopStock/EditPrices/5
        public async Task<IActionResult> EditPrices(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var shopStock = await _context.ShopStock
                .Include(ss => ss.Shop)
                .Include(ss => ss.StockItem)
                .FirstOrDefaultAsync(ss => ss.ShopStockId == id && ss.Shop!.ProprietorUserId == userId);

            if (shopStock == null)
            {
                return NotFound();
            }

            return View(shopStock);
        }

        // POST: ShopStock/EditPrices/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPrices(int id, decimal inheritedSellPrice)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var shopStock = await _context.ShopStock
                .Include(ss => ss.Shop)
                .Include(ss => ss.StockItem)
                .FirstOrDefaultAsync(ss => ss.ShopStockId == id && ss.Shop!.ProprietorUserId == userId);

            if (shopStock == null)
            {
                return NotFound();
            }

            shopStock.InheritedSellPrice = inheritedSellPrice;

            try
            {
                _context.Update(shopStock);
                await _context.SaveChangesAsync();
                TempData["AlertSuccess"] = $"Updated sell price for '{shopStock.StockItem?.Name}'.";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["AlertDanger"] = "An error occurred while updating the price.";
            }

            return RedirectToAction(nameof(Index), new { shopId = shopStock.ShopId });
        }

        // POST: ShopStock/Delete/5
        // this is if we are fully deleting an item of stock - we need to return the stock back to the main inventory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var shopStock = await _context.ShopStock
                .Include(ss => ss.Shop)
                .Include(ss => ss.StockItem)
                .FirstOrDefaultAsync(ss => ss.ShopStockId == id && ss.Shop!.ProprietorUserId == userId);

            if (shopStock == null)
            {
                return NotFound();
            }

            // Return stock to main inventory
            var stockItem = shopStock.StockItem;
            if (stockItem != null)
            {
                stockItem.Quantity += shopStock.QtyOnHand;
            }

            _context.ShopStock.Remove(shopStock);
            await _context.SaveChangesAsync();

            TempData["AlertSuccess"] = $"Removed '{shopStock.StockItem?.Name}' from shop stock. {shopStock.QtyOnHand} units returned to main inventory.";
            return RedirectToAction(nameof(Index), new { shopId = shopStock.ShopId });
        }
    }
}