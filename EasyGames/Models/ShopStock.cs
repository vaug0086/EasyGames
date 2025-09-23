using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EasyGames.Models
{
    public class ShopStock
    {
        public int ShopStockId { get; set; }

        [Required]
        public int ShopId { get; set; }

        [Required]
        public int StockItemId { get; set; }

        [Range(0, 100000)]
        [Display(Name = "Quantity on Hand")]
        public int QtyOnHand { get; set; }

        [Range(0, 100000)]
        [Display(Name = "Low Stock Threshold")]
        public int LowStockThreshold { get; set; }

        [DataType(DataType.Currency)]
        [Precision(18, 2)]
        [Display(Name = "Inherited Buy Price")]
        public decimal InheritedBuyPrice { get; set; }

        [DataType(DataType.Currency)]
        [Precision(18, 2)]
        [Display(Name = "Inherited Sell Price")]
        public decimal InheritedSellPrice { get; set; }

        // Navigation properties
        public Shop? Shop { get; set; }
        public StockItem? StockItem { get; set; }

        // Calculated properties
        [Display(Name = "Is Low Stock")]
        public bool IsLowStock => QtyOnHand <= LowStockThreshold;

        [Display(Name = "Line Value (Buy)")]
        public decimal LineBuyValue => QtyOnHand * InheritedBuyPrice;

        [Display(Name = "Line Value (Sell)")]
        public decimal LineSellValue => QtyOnHand * InheritedSellPrice;
    }
}