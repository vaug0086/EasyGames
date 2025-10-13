using System.Security.Claims;
using EasyGames.Data;
using EasyGames.Models;
using EasyGames.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Controllers
{
    [Authorize(Roles = "Proprietor")]
    public class PosOrdersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public PosOrdersController(ApplicationDbContext db) => _db = db;

        //Get the users shop access. 
        private async Task<List<Shop>> GetUserShopsAsync(string userId) =>
            await _db.Shops.AsNoTracking()
                .Where(s => s.ProprietorUserId == userId)
                .OrderBy(s => s.Name).ToListAsync();

        // Check access to shop
        private async Task<bool> UserOwnsShopAsync(int shopId, string userId) =>
            await _db.Shops.AsNoTracking()
                .AnyAsync(s => s.ShopId == shopId && s.ProprietorUserId == userId);

        // GET: /PosOrders?shopId=&q=&status=&from=&to=&page=1&pageSize=20&sort=newest
        public async Task<IActionResult> Index(
            int? shopId, string? q, string? status, DateTime? from, DateTime? to,
            int page = 1, int pageSize = 20, string sort = "newest")
        {
            //Get users shops
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var shops = await GetUserShopsAsync(uid);
            if (shops.Count == 0) return NotFound("No shop assigned to your account.");

            var shop = shopId.HasValue
                ? shops.FirstOrDefault(s => s.ShopId == shopId.Value)
                : shops.First();

            if (shop is null) return NotFound("Shop not found or not owned by you.");

            //Page constraints
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;
            //Get shop orders
            IQueryable<Order> query = _db.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Where(o => o.Channel == "Shop" && o.ShopId == shop.ShopId);
            //Filter to status
            if (!string.IsNullOrWhiteSpace(status) && OrderStatuses.All.Contains(status))
                query = query.Where(o => o.Status == status);
            //Filter to user
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(o =>
                    EF.Functions.Like(o.Id.ToString(), $"%{term}%") ||
                    (o.User.FullName != null && EF.Functions.Like(o.User.FullName, $"%{term}%")) ||
                    (o.User.Email != null && EF.Functions.Like(o.User.Email, $"%{term}%")) ||
                    (o.User.PhoneNumber != null && EF.Functions.Like(o.User.PhoneNumber, $"%{term}%")));
            }
            //Filter to Date
            if (from.HasValue) query = query.Where(o => o.CreatedUtc >= from.Value);
            if (to.HasValue) query = query.Where(o => o.CreatedUtc < to.Value.AddDays(1));
            //Sort query
            query = sort switch
            {
                "oldest" => query.OrderBy(o => o.CreatedUtc),
                "total" => query.OrderByDescending(o => o.GrandTotal),
                _ => query.OrderByDescending(o => o.CreatedUtc)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new PosOrdersIndexVm
            {
                UserShops = shops,
                SelectedShop = shop,
                Items = items,
                TotalCount = totalCount,
                Q = q,
                Status = status,
                From = from,
                To = to,
                Page = page,
                PageSize = pageSize,
                Sort = sort
            };

            return View(vm);
        }

        // GET: /PosOrders/Details/123?shopId=45
        public async Task<IActionResult> Details(int id, int shopId)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await UserOwnsShopAsync(shopId, uid)) return Forbid();

            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(i => i.StockItem)
                .FirstOrDefaultAsync(o => o.Id == id && o.Channel == "Shop" && o.ShopId == shopId);

            return order is null ? NotFound() : View(order);
        }
    }
}
