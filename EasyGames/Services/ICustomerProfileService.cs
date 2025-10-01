using EasyGames.Models;

namespace EasyGames.Services
{
    public interface ICustomerProfileService
    {
        Task<CustomerProfile> GetOrCreateAsync(string userId);
        Task UpdateAfterSaleAsync(string userId, decimal saleProfit);
    }
}
