namespace EasyGames.Models
{   //  holds stockitemid to link back to the product entity
    //  stores name unitprice and quantity at the time of adding to cart
    //  exposes a linetotal computed property (unitprice multiplied by the  quantity) so totals can be calculated easily
    public sealed class CartItem
    {
        public int StockItemId { get; init; }
        public string Name { get; init; } = "";
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
