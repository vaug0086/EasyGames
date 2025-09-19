using EasyGames.Models;

namespace EasyGames.Services
{
    //  what this interface defines
    //  add updateqty and remove modify cart contents by stockitemid
    //  getitems getcount and getsubtotal expose the current cart state
    //  clear empties the cart
    //  mergeanoncartintocurrentuser moves items from an anonymous session cart into the logged in user cart
    public interface ICartService
    {
        void Add(int stockItemId, int qty = 1);
        void UpdateQty(int stockItemId, int qty);
        void Remove(int stockItemId);
        IReadOnlyList<CartItem> GetItems();
        int GetCount();
        decimal GetSubtotal();
        void Clear();
        void MergeAnonCartIntoCurrentUser();
    }
}

