using EasyGames.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
//  What dis is
//  orderscontroller, viewing a user’s past orders
//  secured with [authorize], so only authenticated users can access these actions
//  index()  looks up current user id from claims (claimtypes.nameidentifier), queries orders table
//  where userid matches, orders them descending by id, returns list<order> to the view.
//  details(id) includes related orderitems and stockitems but only if the order’s userid
//  matches the current loggedin user
//  uses asnotracking() on queries since nobody dont modify entities here
namespace EasyGames.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public OrdersController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var orders = await _db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.Id)
                .AsNoTracking()
                .ToListAsync();
            return View(orders); // passes List<Order>
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            var order = await _db.Orders
                .Include(o => o.Items).ThenInclude(i => i.StockItem)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order is null) return NotFound();
            return View(order);
        }
    }
}
