namespace EasyGames.Models
{
    //  links to its parent order through orderid and navigation property
    //  links to the purchased stockitem so product details can be retrieved
    //  records quantity and the unitpriceatpurchase which snapshots the price at the time of checkout
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = default!;

        public int StockItemId { get; set; }
        public StockItem StockItem { get; set; } = default!;

        public int Quantity { get; set; }
        public decimal UnitPriceAtPurchase { get; set; }
    }

}
