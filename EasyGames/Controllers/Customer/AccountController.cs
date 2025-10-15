using EasyGames.Data;
using EasyGames.Models;             // <- for Tier enum
using EasyGames.Services;
using EasyGames.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace EasyGames.Controllers.Customer
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ICustomerProfileService _profiles;
        private readonly ITierService _tiers;

        public AccountController(
            ApplicationDbContext db,
            ICustomerProfileService profiles,
            ITierService tiers)
        {
            _db = db; _profiles = profiles; _tiers = tiers;
        }

        
        private static decimal NextTarget(Tier t) => t switch
        {
            Tier.Bronze => 200m,
            Tier.Silver => 500m,
            _ => 0m
        };

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
           

            // choose which table to show
            var isStaff = User.IsInRole("Admin");

            // Recalculate lifetime from orders (choose what counts)
            var lifetime = (await _db.Orders.AsNoTracking()
          .Where(o => o.UserId == userId && o.Status == OrderStatuses.Fulfilled )
          .Select(o => o.GrandTotal)  
          .ToListAsync())
          .DefaultIfEmpty(0m)
          .Sum();


            //  Load/create profile, update if changed, and persist
            var profile = await _profiles.GetOrCreateAsync(userId);
            if (profile.LifetimeProfitContribution != lifetime)
            {
                profile.LifetimeProfitContribution = lifetime;
                profile.CurrentTier = _tiers.ComputeTier(lifetime); 
                await _db.SaveChangesAsync();
            }

            // UI numbers
            var target = NextTarget(profile.CurrentTier);
            var progressPct = target <= 0m
                ? 100
                : (int)Math.Clamp((double)((profile.LifetimeProfitContribution / target) * 100m), 0, 100);

            // Table rows (safe placeholders for staff-only cols)
            var rows = await _db.Orders.AsNoTracking()
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedUtc)      
                .Select(o => new PurchaseRowVM
                {
                    CreatedUtc = o.CreatedUtc,
                    Channel = "Web",
                    Total = o.GrandTotal 
                })
                .ToListAsync();

            var vm = new MyAccountViewModel
            {
                CurrentTier = profile.CurrentTier.ToString(),
                LifetimeContribution = profile.LifetimeProfitContribution,
                NextTierTarget = target,
                ProgressPercent = progressPct,
                IsStaff = isStaff,
                Rows = rows
            };

            return View(vm);
        }

    }
}