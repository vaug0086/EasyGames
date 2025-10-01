using EasyGames.Models;

namespace EasyGames.Services
{
    public interface ISalesService
    {
        Task<Order> CompleteSaleAsync(string? userId, string channel, int? shopId, List<OrderItem> items);
    }
}
