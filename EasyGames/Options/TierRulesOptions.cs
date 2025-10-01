using EasyGames.Models;

namespace EasyGames.Options
{
    public class TierRulesOptions
    {
        public decimal SilverMinProfit { get; set; } = 200m;
        public decimal GoldMinProfit { get; set; } = 1000m;
        public decimal PlatinumMinProfit { get; set; } = 3000m;

        public Dictionary<string, int> Discounts { get; set; } = new()
        {
            { nameof(Tier.Bronze), 0 },
            { nameof(Tier.Silver), 5 },
            { nameof(Tier.Gold), 10 },
            { nameof(Tier.Platinum), 15 }
        };
    }
}
