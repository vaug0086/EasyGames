using System.Text.Json;
using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
//POS Cart/basket mostly mirroring the online cart version but kept separate.
// provides add/update/remove/clear and summary for the POS cart/basket session.
// similar and quick server side state using asp.net core sessions as the Cart service
namespace EasyGames.Services
{
    public sealed class PosCartService : IPosCartService
    {
        private readonly ISession _session;
        private readonly ApplicationDbContext _db;

        public PosCartService(IHttpContextAccessor accessor, ApplicationDbContext db)
        {
            _session = accessor.HttpContext!.Session;
            _db = db;
        }

        private static string Key(int shopId) => $"pos:{shopId}";
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public IReadOnlyList<CartItem> GetItems(int shopId)
        {
            var data = _session.GetString(Key(shopId));
            return string.IsNullOrEmpty(data)
                ? new List<CartItem>()
                : (JsonSerializer.Deserialize<List<CartItem>>(data, _json) ?? new List<CartItem>());
        }

        private void Save(int shopId, List<CartItem> items)
            => _session.SetString(Key(shopId), JsonSerializer.Serialize(items, _json));

        public void Add(int shopId, int stockItemId, int qty)
        {
            if (qty < 1) qty = 1;
            var items = GetItems(shopId).ToList();

            var existing = items.FirstOrDefault(i => i.StockItemId == stockItemId);
            if (existing != null)
            {
                existing.Quantity += qty;
                Save(shopId, items);
                return;
            }

            // Take the price from the shopstock but incase of error go back to stockpice
            var ss = _db.ShopStock
                        .AsNoTracking()
                        .Include(x => x.StockItem)
                        .FirstOrDefault(x => x.ShopId == shopId && x.StockItemId == stockItemId);

            string name;
            decimal price;

            if (ss != null)
            {
                name = ss.StockItem!.Name;
                price = ss.InheritedSellPrice;
            }
            else
            {
                var si = _db.StockItems.AsNoTracking().First(s => s.Id == stockItemId);
                name = si.Name;
                price = si.SellPrice;
            }

            items.Add(new CartItem
            {
                StockItemId = stockItemId,
                Name = name,
                UnitPrice = price,
                Quantity = qty
            });

            Save(shopId, items);
        }

        public void UpdateQty(int shopId, int stockItemId, int qty)
        {
            var items = GetItems(shopId).ToList();
            var existing = items.FirstOrDefault(i => i.StockItemId == stockItemId);
            if (existing == null) return;

            if (qty <= 0)
                items.Remove(existing);
            else
                existing.Quantity = qty;

            Save(shopId, items);
        }

        public void Remove(int shopId, int stockItemId)
        {
            var items = GetItems(shopId).ToList();
            items.RemoveAll(i => i.StockItemId == stockItemId);
            Save(shopId, items);
        }

        public void Clear(int shopId) => Save(shopId, new List<CartItem>());

        public decimal Subtotal(int shopId) => GetItems(shopId).Sum(i => i.LineTotal);
    }
}
