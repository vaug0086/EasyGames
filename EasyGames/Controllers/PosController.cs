using System.Security.Claims;
using EasyGames.Data;
using EasyGames.Models;
using EasyGames.Services;
using EasyGames.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Controllers
{
    [Authorize(Roles = "Proprietor")]
    public class PosController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPosCartService _posCart;

        public PosController(ApplicationDbContext db, IPosCartService posCart)
        {
            _db = db;
            _posCart = posCart;
        }

        private async Task<List<Shop>> GetUserShopsAsync(string userId)
            => await _db.Shops
                .AsNoTracking()
                .Where(s => s.ProprietorUserId == userId)
                .OrderBy(s => s.Name)
                .ToListAsync();

        private async Task<Shop?> ResolveShopAsync(int? shopId, string userId, List<Shop> userShops)
        {
            if (shopId.HasValue)
                return userShops.FirstOrDefault(s => s.ShopId == shopId);

            return userShops.FirstOrDefault(); // default to first shop
        }



        [HttpGet]
        public async Task<IActionResult> Index(int? shopId, StockCategory? category)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userShops = await GetUserShopsAsync(uid);
            //checking against staff assigned to shop
            if (!userShops.Any()) return NotFound("No shop assigned to your account.");

            var shop = await ResolveShopAsync(shopId, uid, userShops);
            if (shop is null) return NotFound("Shop not found or not owned by you.");

            var stock = await _db.ShopStock
                .AsNoTracking()
                .Include(ss => ss.StockItem)
                .Where(ss => ss.ShopId == shop.ShopId)
                .OrderBy(ss => ss.StockItem!.Category)
                .ThenBy(ss => ss.StockItem!.Name)
                .ToListAsync();

            var basket = _posCart.GetItems(shop.ShopId).ToList();

            var vm = new PosIndexVm
            {
                SelectedShop = shop,
                UserShops = userShops,
                Stock = stock,
                Basket = basket,
                Subtotal = _posCart.Subtotal(shop.ShopId)
            };

            if (category is null)
            {
                // View for Categories
                vm.Mode = PosScreenMode.Categories;

                // Categories available for this shop and items
                var catCounts = await _db.ShopStock
                    .AsNoTracking()
                    .Include(ss => ss.StockItem)
                    .Where(ss => ss.ShopId == shop.ShopId)
                    .GroupBy(ss => ss.StockItem!.Category)
                    .Select(g => new CategorySummary { Category = g.Key, ItemCount = g.Count() })
                    .ToListAsync();

                // Makes sure all categories are shown even if empty to allow sale of items without stock
                var allCats = Enum.GetValues(typeof(StockCategory)).Cast<StockCategory>();
                foreach (var c in allCats)
                    if (!catCounts.Any(x => x.Category == c))
                        catCounts.Add(new CategorySummary { Category = c, ItemCount = 0 });

                // Order by name
                vm.Categories = catCounts.OrderBy(x => x.Category.ToString()).ToList();
            }
            else
            {
                // Show items for selected category
                vm.Mode = PosScreenMode.Items;
                vm.SelectedCategory = category;

                vm.Stock = await _db.ShopStock
                    .AsNoTracking()
                    .Include(ss => ss.StockItem)
                    .Where(ss => ss.ShopId == shop.ShopId && ss.StockItem!.Category == category.Value)
                    .OrderBy(ss => ss.StockItem!.Name)
                    .ToListAsync();
            }


            return View(vm);
        }

        //Verifies shop owner access
        private async Task<bool> UserOwnsShopAsync(int shopId, string userId)
        {
            return await _db.Shops
                .AsNoTracking()
                .AnyAsync(s => s.ShopId == shopId && s.ProprietorUserId == userId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>Add(int shopId, int stockItemId, int qty = 1, string? returnUrl = null)
        {
            //verify access
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await UserOwnsShopAsync(shopId, uid)) return Forbid();

            _posCart.Add(shopId, stockItemId, qty);
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index), new { shopId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int shopId, int stockItemId, int qty)
        {
            //verify access
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await UserOwnsShopAsync(shopId, uid)) return Forbid();

            _posCart.UpdateQty(shopId, stockItemId, qty);
            return RedirectToAction(nameof(Index), new { shopId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int shopId, int stockItemId)
        {
            //verify access
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await UserOwnsShopAsync(shopId, uid)) return Forbid();

            _posCart.Remove(shopId, stockItemId);
            return RedirectToAction(nameof(Index), new { shopId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear(int shopId)
        {
            //verify access
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await UserOwnsShopAsync(shopId, uid)) return Forbid();

            _posCart.Clear(shopId);
            return RedirectToAction(nameof(Index), new { shopId });
        }
    }
}
