using System.Collections.Generic;
using EasyGames.Models;

namespace EasyGames.ViewModels
{
    public enum PosScreenMode { Categories, Items }

    public class PosIndexVm
    {
        public Shop SelectedShop { get; set; } = default!;
        public List<Shop> UserShops { get; set; } = new();

        // Categories view
        public PosScreenMode Mode { get; set; } = PosScreenMode.Categories;
        public List<CategorySummary> Categories { get; set; } = new();

        // Items/Stock view
        public StockCategory? SelectedCategory { get; set; }
        public List<ShopStock> Stock { get; set; } = new();

        // Basket
        public List<CartItem> Basket { get; set; } = new();
        public decimal Subtotal { get; set; }

        //Discount stuff
        public string? CustomerPhone { get; set; }
        public string? CustomerName { get; set; }
        public decimal? DiscountAmount { get; set; }
    }

    public class CategorySummary
    {
        public StockCategory Category { get; set; }
        public int ItemCount { get; set; }
    }
}
