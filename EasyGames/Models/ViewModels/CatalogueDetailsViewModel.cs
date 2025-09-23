namespace EasyGames.Models.ViewModels
{
    public class CatalogueDetailsViewModel
    {
        public required StockItem StockItem { get; set; }
        public List<ShopStockViewModel> ShopStock { get; set; } = new();
    }

    public class ShopStockViewModel
    {
        public required string ShopName { get; set; }
        public required string ShopAddress { get; set; }
        public int QtyOnHand { get; set; }
        public bool IsLowStock { get; set; }
    }
}