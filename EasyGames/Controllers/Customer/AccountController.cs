using EasyGames.Data;
using EasyGames.Services;
using EasyGames.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Controllers.Customer
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ICustomerProfileService _profiles;
        private readonly ITierService _tiers;

        public AccountController(ApplicationDbContext db, ICustomerProfileService profiles, ITierService tiers)
        {
            _db = db; _profiles = profiles; _tiers = tiers;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            var profile = await _profiles.GetOrCreateAsync(userId);

            var sales = await _db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedUtc)
                .Select(o => new MyAccountViewModel.SaleRow
                {
                    Id = o.Id,
                    Date = o.CreatedUtc,
                    Channel = o.Channel,
                    TotalSell = o.GrandTotal,
                    TotalCost = o.TotalCost,       // add these so your view compiles
                    TotalProfit = o.TotalProfit
                })
                .ToListAsync();

            var (_, next, pct) = _tiers.Progress(profile.LifetimeProfitContribution, profile.CurrentTier);

            var vm = new MyAccountViewModel
            {
                CurrentTier = profile.CurrentTier,
                LifetimeProfit = profile.LifetimeProfitContribution,
                PercentToNext = (decimal)pct,       // <-- cast double -> decimal
                NextTierTarget = (decimal)next,     // <-- cast if next is double too
                Sales = sales
            };

            return View(vm);
        }

    }
}

