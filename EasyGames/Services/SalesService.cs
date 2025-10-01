using EasyGames.Data;
using EasyGames.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Services
{
    public class SalesService : ISalesService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICustomerProfileService _profiles;

        public SalesService(ApplicationDbContext db, ICustomerProfileService profiles)
        {
            _db = db; _profiles = profiles;
        }
        public Task<Order> CompleteSaleAsync(string? userId, string channel, int? shopId, List<OrderItem> items)
        {
            // Forward to the new method; adjust defaults if you added discount/negative flags there
            return CompleteOrderAsync(userId, channel, shopId, items);
        }


        // works with Order / OrderItem
        public async Task<Order> CompleteOrderAsync(
            string? userId,
            string channel,
            int? shopId,
            List<OrderItem> items)
        {
            // compute totals from ORDER ITEMS
            var totalSell = items.Sum(i => i.UnitPriceAtPurchase * i.Quantity);      // sell snapshot
            var totalCost = items.Sum(i => i.UnitBuyPriceAtPurchase * i.Quantity);      // buy snapshot
            var totalProfit = totalSell - totalCost;

            var order = new Order
            {
                UserId = userId ?? "",               // if you support true guests, make UserId nullable in Order
                Channel = channel,                    // "Web" or "Shop"
                ShopId = shopId,                     // null for web
                CreatedUtc = DateTime.UtcNow,

                
                Subtotal = totalSell,
                GrandTotal = totalSell,

                TotalCost = totalCost,
                TotalProfit = totalProfit,

                Status = OrderStatuses.Fulfilled,    // or Pending if you ship later
                Items = items
            };

            _db.Orders.Add(order);

            // TODO: decrement inventory here
            //  - if channel == "Web": decrement StockItem.Quantity
            //  - if channel == "Shop": decrement ShopStock.QtyOnHand (optionally allow negative)

            await _db.SaveChangesAsync();

            if (!string.IsNullOrEmpty(userId))
            {
                // updates LifetimeProfitContribution and CurrentTier
                await _profiles.UpdateAfterSaleAsync(userId, totalProfit);
            }

            return order;
        }
    }
}
