using System.Collections.Generic;
using EasyGames.Models;

namespace EasyGames.Services
{
    //  Defines add, remove, clear and update by sockitemid and subtotal for the POS session basket)
    //  Clear empties the Basket
    public interface IPosCartService
    {
        IReadOnlyList<CartItem> GetItems(int shopId);
        void Add(int shopId, int stockItemId, int qty);
        void UpdateQty(int shopId, int stockItemId, int qty);
        void Remove(int shopId, int stockItemId);
        void Clear(int shopId);
        decimal Subtotal(int shopId);
    }
}
