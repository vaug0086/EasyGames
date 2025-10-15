using EasyGames.Data;
using EasyGames.Models;
using System.Linq;
using EasyGames.Services;
using EasyGames.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    private readonly ICustomerProfileService _profiles;
    private readonly ITierService _tiers;

    public OrdersAdminController(ApplicationDbContext db, ICustomerProfileService profiles, ITierService tiers)
    {
        _db = db;
        _profiles = profiles;
        _tiers = tiers;
    }

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

        // Get unique user ids in this page
        var userIds = items.Select(i => i.UserId).Distinct().ToList();

        // Look up their profiles and build a dictionary: UserId -> Tier
        var tiersByUserId = await _db.CustomerProfiles.AsNoTracking()
            .Where(p => userIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, p => p.CurrentTier);

        return View(new OrdersAdminIndexVm
        {
            Items = items,
            TiersByUserId = tiersByUserId,
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
    //  Modified this so that if you update an order status to cancelled
    //  the stock is returned. Probably should add a clamp to prevent
    //  going from cancelled to fufilled, too bad!
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(status) || !OrderStatuses.All.Contains(status))
        {
            TempData["AlertDanger"] = "Invalid status.";
            return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Index))!);
        }

        var order = await _db.Orders
            .Include(o => o.Items)
            .ThenInclude(oi => oi.StockItem)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();

        if (string.Equals(order.Status, status, StringComparison.Ordinal))
        {
            TempData["AlertInfo"] = $"Order #{order.Id} already {status}.";
            return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Index))!);
        }

        var illegalToCancel = new[] { "Shipped", "Completed" };
        if (status == "Cancelled" && illegalToCancel.Contains(order.Status))
        {
            TempData["AlertDanger"] = $"Cannot cancel an order in status '{order.Status}'.";
            return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Index))!);
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            //  Handle the cancellation. If the order is cancelled, return the stock for each item
            if (status == "Cancelled" && !order.StockReturnedOnCancel)
            {
                foreach (var line in order.Items)
                {
                    if (line.StockItem is null)
                        line.StockItem = await _db.StockItems.FirstOrDefaultAsync(s => s.Id == line.StockItemId);

                    if (line.StockItem is not null)
                        line.StockItem.Quantity += line.Quantity;
                }
                //  Prevent the order from being double canceled.
                order.StockReturnedOnCancel = true;
            }

            order.Status = status;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["AlertSuccess"] = $"Order #{order.Id} set to {status}."
                + (status == "Cancelled" && order.StockReturnedOnCancel ? " Stock returned." : "");
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            TempData["AlertDanger"] = "The order was modified by another process. Please try again.";
        }

        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Index))!);
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


    [HttpPost]
    public async Task<IActionResult> Fulfill(int id)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();

        order.Status = OrderStatuses.Fulfilled;
        await _db.SaveChangesAsync();

        // Recompute from fulfilled orders only
        var lifetime = await _db.Orders
            .Where(o => o.UserId == order.UserId && o.Status == OrderStatuses.Fulfilled)
            .SumAsync(o => (decimal?)o.GrandTotal) ?? 0m;

        var profile = await _profiles.GetOrCreateAsync(order.UserId);
        profile.LifetimeProfitContribution = lifetime;
        profile.CurrentTier = _tiers.ComputeTier(lifetime);
        await _db.SaveChangesAsync();

        return RedirectToAction("Details", new { id }); // or wherever you want to go
    }
}

