using System.Security.Claims;
using EasyGames.Data;
using EasyGames.Models;
using EasyGames.Services;
using EasyGames.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace EasyGames.Controllers
{
    [Authorize(Roles = "Proprietor")]
    public class PosController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPosCartService _posCart;
        private readonly IPosStateService _posState;
        private readonly ICustomerProfileService _profiles;
        private readonly ITierService _tiers;
        private readonly UserManager<ApplicationUser> _userManager;

        public PosController(ApplicationDbContext db,
                             IPosCartService posCart,
                             IPosStateService posState,
                             ICustomerProfileService profiles,
                             ITierService tiers,
                             UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _posCart = posCart;
            _posState = posState;
            _profiles = profiles;
            _tiers = tiers;
            _userManager = userManager;
        }
        //Gets the users access to shops for POS system(allows users to work at multiple shops)
        private async Task<List<Shop>> GetUserShopsAsync(string userId)
            => await _db.Shops
                .AsNoTracking()
                .Where(s => s.ProprietorUserId == userId)
                .OrderBy(s => s.Name)
                .ToListAsync();

        //resolves shop to defualt
        private async Task<Shop?> ResolveShopAsync(int? shopId, string userId, List<Shop> userShops)
        {
            if (shopId.HasValue)
                return userShops.FirstOrDefault(s => s.ShopId == shopId);

            return userShops.FirstOrDefault(); // default to first shop
        }
        //Defualt Guestuseraccount for POS orders without an attached user to the order
        private const string GuestEmail = "guest@easygames.com";
        private async Task<string> GetCheckoutUserIdAsync(int shopId)
        {
            // If a customer is attached, use them
            var attachedId = _posState.GetCustomerId(shopId);
            if (!string.IsNullOrEmpty(attachedId)) return attachedId;

            // Else use the pre-seeded Guest user
            var guest = await _userManager.FindByEmailAsync(GuestEmail);
            if (guest == null)
                throw new InvalidOperationException(
                    $"Guest user '{GuestEmail}' not found. something is likely broken in the DB. Reseed or update guest email");

            return guest.Id;
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

            //attach customer discount if entitled 
            var attachedId = _posState.GetCustomerId(shop.ShopId);
            if (!string.IsNullOrEmpty(attachedId))
            {
                var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == attachedId);
                if (user != null)
                {
                    var profile = await _profiles.GetOrCreateAsync(user.Id);

                    // discount mappings
                    decimal discountPct = profile.CurrentTier switch
                    {
                        Tier.Silver => 0.05m,
                        Tier.Gold => 0.10m,
                        Tier.Platinum => 0.15m,
                        _ => 0.00m
                    };

                    vm.CustomerPhone = _posState.GetCustomerPhone(shop.ShopId);
                    vm.CustomerName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName;
                    vm.DiscountAmount = decimal.Round(vm.Subtotal * discountPct, 2, MidpointRounding.AwayFromZero);
                }
            }
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
        //Used to attach a customer to the current order via Phonenumbe. 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AttachCustomer(int shopId, string mobile, string? returnCategory)
        {
            var uid = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!;
            if (!await UserOwnsShopAsync(shopId, uid)) return Forbid();

            var input = (mobile ?? "").Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                TempData["AlertDanger"] = "Enter a mobile number.";
                return RedirectToAction(nameof(Index), new { shopId, category = returnCategory });
            }

            // Exact match against Db numbers
            var user = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == input);

            if (user is null)
            {
                TempData["AlertInfo"] = "No account with that mobile. You can still sell as guest or sign them up.";
                _posState.ClearCustomer(shopId);
                return RedirectToAction(nameof(Index), new { shopId, category = returnCategory });
            }

            _posState.SetCustomer(shopId, user.Id, user.PhoneNumber ?? input);
            TempData["AlertSuccess"] = $"Customer attached: {(string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName)}";
            return RedirectToAction(nameof(Index), new { shopId, category = returnCategory });
        }
        //Used to remove a customer from the current POS order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DetachCustomer(int shopId, string? returnCategory)
        {
            var uid = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!;
            if (!await UserOwnsShopAsync(shopId, uid)) return Forbid();

            _posState.ClearCustomer(shopId);
            TempData["AlertInfo"] = "Customer detached.";
            return RedirectToAction(nameof(Index), new { shopId, category = returnCategory });
        }

        //Used to Checkout for POS System
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int shopId, string? returnCategory)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await UserOwnsShopAsync(shopId, uid)) return Forbid();
            //Check for empty basket
            var basket = _posCart.GetItems(shopId).ToList();
            if (basket.Count == 0)
            {
                TempData["AlertDanger"] = "Basket is empty.";
                return RedirectToAction(nameof(Index), new { shopId, category = returnCategory });
            }

            //Prefer ShopStock prices, fallback to StockItem if missing
            var stockItemIds = basket.Select(b => b.StockItemId).ToList();
            var shopStocks = await _db.ShopStock
                .Include(ss => ss.StockItem)
                .Where(ss => ss.ShopId == shopId && stockItemIds.Contains(ss.StockItemId))
                .ToListAsync();
            var stockItems = await _db.StockItems
                .Where(si => stockItemIds.Contains(si.Id))
                .ToListAsync();

            // Subtotal at current sell price
            decimal subtotal = 0m;
            decimal totalCost = 0m;

            // Discount if attached customer has one
            decimal discountPct = 0m;
            var attachedId = _posState.GetCustomerId(shopId);
            if (!string.IsNullOrEmpty(attachedId))
            {
                var profile = await _profiles.GetOrCreateAsync(attachedId);
                discountPct = profile.CurrentTier switch
                {
                    Tier.Silver => 0.05m,
                    Tier.Gold => 0.10m,
                    Tier.Platinum => 0.15m,
                    _ => 0m
                };
            }

            var order = new Order
            {
                UserId = await GetCheckoutUserIdAsync(shopId),
                CreatedUtc = DateTime.UtcNow,
                ShippingName = "POS Sale",
                ShippingAddress = "",
                Channel = "Shop",
                ShopId = shopId,
                Status = OrderStatuses.Pending // Will be set to Fulfilled if no backorders
            };

            foreach (var line in basket)
            {
                var ss = shopStocks.FirstOrDefault(x => x.StockItemId == line.StockItemId);
                var si = stockItems.FirstOrDefault(x => x.Id == line.StockItemId);

                var sell = ss?.InheritedSellPrice ?? si?.SellPrice ?? line.UnitPrice;
                var buy = ss?.InheritedBuyPrice ?? si?.BuyPrice ?? 0m;

                subtotal += sell * line.Quantity;
                totalCost += buy * line.Quantity;

                // Calculate backorder quantity
                int backordered = 0;
                if (ss != null)
                {
                    var before = ss.QtyOnHand;
                    var newQty = before - line.Quantity;

                    if (newQty < 0)
                    {
                        // Calculate how many units are backordered
                        backordered = -newQty;
                        ss.QtyOnHand = 0;

                        // Warning with backorder notification
                        TempData["AlertWarning"] = $"{backordered} unit(s) of '{ss.StockItem?.Name}' placed on backorder.";
                    }
                    else
                    {
                        ss.QtyOnHand = newQty;
                    }
                }

                order.Items.Add(new OrderItem
                {
                    StockItemId = line.StockItemId,
                    Quantity = line.Quantity,
                    UnitPriceAtPurchase = sell,
                    UnitBuyPriceAtPurchase = buy,
                    QuantityBackordered = backordered // 0, unless overordered
                });
            }

            var discount = decimal.Round(subtotal * discountPct, 2, MidpointRounding.AwayFromZero);
            var grand = subtotal - discount;

            order.Subtotal = decimal.Round(subtotal, 2, MidpointRounding.AwayFromZero);
            order.GrandTotal = decimal.Round(grand, 2, MidpointRounding.AwayFromZero);
            order.TotalCost = decimal.Round(totalCost, 2, MidpointRounding.AwayFromZero);
            order.TotalProfit = order.GrandTotal - order.TotalCost;

            // Set order status based on backorder status
            var hasBackorders = order.Items.Any(i => i.QuantityBackordered > 0); // checks if any items have backordered quantity >0
            // ? is ternary conditional operator - sets order.Status based on whether hasBackorders is true or false
            // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/conditional-operator
            order.Status = hasBackorders ? OrderStatuses.Pending : OrderStatuses.Fulfilled;

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            _posCart.Clear(shopId);
            // Clear attached customer for next sale
            _posState.ClearCustomer(shopId);

            TempData["AlertSuccess"] = $"POS sale complete. Order #{order.Id} total {order.GrandTotal:C}.";
            return RedirectToAction(nameof(Index), new { shopId, category = returnCategory });
        }
    }
}
