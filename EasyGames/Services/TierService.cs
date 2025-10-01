using EasyGames.Models;
using EasyGames.Options;
using Microsoft.Extensions.Options;

namespace EasyGames.Services
{
    public class TierService : ITierService
    {
        private readonly TierRulesOptions _rules;
        public TierService(IOptions<TierRulesOptions> rules) => _rules = rules.Value;

        public Tier ComputeTier(decimal lifetimeProfit)
        {
            if (lifetimeProfit >= _rules.PlatinumMinProfit) return Tier.Platinum;
            if (lifetimeProfit >= _rules.GoldMinProfit) return Tier.Gold;
            if (lifetimeProfit >= _rules.SilverMinProfit) return Tier.Silver;
            return Tier.Bronze;
        }

        public (decimal current, decimal nextTarget, double percentToNext) Progress(decimal lifetimeProfit, Tier current)
        {
            decimal next = current switch
            {
                Tier.Bronze => _rules.SilverMinProfit,
                Tier.Silver => _rules.GoldMinProfit,
                Tier.Gold => _rules.PlatinumMinProfit,
                _ => _rules.PlatinumMinProfit
            };

            if (current == Tier.Platinum) return (lifetimeProfit, next, 1.0);
            if (next <= 0) return (lifetimeProfit, 0, 1.0);

            var pct = (double)Math.Clamp(lifetimeProfit / next, 0, 1);
            return (lifetimeProfit, next, pct);
        }

        public int GetDiscountPercent(Tier tier)
        {
            var key = tier.ToString();
            return _rules.Discounts.TryGetValue(key, out var v) ? v : 0;
        }
    }
}
