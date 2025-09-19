using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EasyGames.Data;
using EasyGames.Models;
using EasyGames.ViewModels;

namespace EasyGames.Controllers;

//  what this controller does
//  secured with authorize roles admin so only admins can access
//  index builds an ef core queryable of orders with filters for status query text date range paging and sorting
//  updatestatus validates status applies changes saves and redirects safely
//  details loads the order with user and items for admin inspection

[Authorize(Roles = "Admin")]
public class OrdersAdminController : Controller
{
    private readonly ApplicationDbContext _db;
    public OrdersAdminController(ApplicationDbContext db) => _db = db;

    // /OrdersAdmin?status=&q=&from=&to=&page=1&pageSize=20&sort=newest
    public async Task<IActionResult> Index(
        string? status, string? q, DateTime? from, DateTime? to,
        int page = 1, int pageSize = 20, string? sort = "newest")
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        IQueryable<Order> query = _db.Orders
            .AsNoTracking()
            .Include(o => o.User);

        if (!string.IsNullOrWhiteSpace(status) && OrderStatuses.All.Contains(status))
            query = query.Where(o => o.Status == status);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(o =>
                EF.Functions.Like(o.Id.ToString(), $"%{term}%") ||
                (o.User.FullName != null && EF.Functions.Like(o.User.FullName, $"%{term}%")) ||
                (o.User.Email != null && EF.Functions.Like(o.User.Email, $"%{term}%")));
        }

        if (from.HasValue) query = query.Where(o => o.CreatedUtc >= from.Value);
        if (to.HasValue) query = query.Where(o => o.CreatedUtc < to.Value.AddDays(1));

        query = sort switch
        {
            "oldest" => query.OrderBy(o => o.CreatedUtc),
            "total" => query.OrderByDescending(o => o.GrandTotal),
            _ => query.OrderByDescending(o => o.CreatedUtc)
        };

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return View(new OrdersAdminIndexVm
        {
            Items = items,
            Status = status,
            Q = q,
            From = from,
            To = to,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Sort = sort
        });
    }

    // POST: /OrdersAdmin/UpdateStatus

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? returnUrl = null)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound();

        if (!OrderStatuses.All.Contains(status))
        {
            TempData["AlertDanger"] = "Invalid status.";
        }
        else if (!string.Equals(order.Status, status, StringComparison.Ordinal))
        {
            order.Status = status;
            await _db.SaveChangesAsync();
            TempData["AlertSuccess"] = $"Order #{order.Id} set to {status}.";
        }
        else
        {
            TempData["AlertInfo"] = $"Order #{order.Id} already {status}.";
        }

        // Only redirect to a local, valid URL. Otherwise go back to Index.
        var fallback = Url.Action(nameof(Index))!;
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return Redirect(fallback);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
                .ThenInclude(oi => oi.StockItem)
            .FirstOrDefaultAsync(o => o.Id == id);

        return order is null ? NotFound() : View(order);
    }
}

