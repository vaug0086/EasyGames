using EasyGames.Models;

namespace EasyGames.Services
{
    public interface ITierService
    {
        Tier ComputeTier(decimal lifetimeProfit);
        (decimal current, decimal nextTarget, double percentToNext) Progress(decimal lifetimeProfit, Tier current);
        int GetDiscountPercent(Tier tier);
    }
}
