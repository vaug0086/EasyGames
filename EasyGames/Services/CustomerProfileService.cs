using EasyGames.Data;
using EasyGames.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Services
{
    public class CustomerProfileService : ICustomerProfileService
    {
        private readonly ApplicationDbContext _db;
        private readonly ITierService _tiers;

        public CustomerProfileService(ApplicationDbContext db, ITierService tiers)
        {
            _db = db; _tiers = tiers;
        }

        public async Task<CustomerProfile> GetOrCreateAsync(string userId)
        {
            var p = await _db.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
            if (p == null)
            {
                p = new CustomerProfile { UserId = userId, CurrentTier = Tier.Bronze, LifetimeProfitContribution = 0m };
                _db.CustomerProfiles.Add(p);
                await _db.SaveChangesAsync();
            }
            return p;
        }

        public async Task UpdateAfterSaleAsync(string userId, decimal saleProfit)
        {
            var p = await GetOrCreateAsync(userId);
            p.LifetimeProfitContribution += saleProfit;
            p.CurrentTier = _tiers.ComputeTier(p.LifetimeProfitContribution);
            await _db.SaveChangesAsync();
        }
    }
}
