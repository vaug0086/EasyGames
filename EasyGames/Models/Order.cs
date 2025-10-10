using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyGames.Models
{
    //  defines orderstatuses constants, a readonly list all for safe whitelisting
    //  order entity holds identity user reference shipping detail wwith a collection of or deritems
    //  createdutc records when the order was placed using utcnow (Could be in local time but whatever) for consistency
    //  status is validated with required stringlength and a regex to enforce only the three allowed values
    //  subtotal and grandtotal are stored separately so you can later add tax or shipping adjustments
    public static class OrderStatuses
    {
        public const string Pending = "Pending";
        public const string Fulfilled = "Fulfilled";
        public const string Cancelled = "Cancelled";

        public static readonly IReadOnlyList<string> All = new[]
        {
            Pending, Fulfilled, Cancelled
        };
    }

    public class Order
    {
        public int Id { get; set; }

        [Required] public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = default!;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        [Required, StringLength(100)] public string ShippingName { get; set; } = "";
        [Required, StringLength(200)] public string ShippingAddress { get; set; } = "";

        public decimal Subtotal { get; set; }
        public decimal GrandTotal { get; set; }

       // // NEW: store where this order happened
        [StringLength(10)]
        public string Channel { get; set; } = "Web"; // "Web" or "Shop"

        public int? ShopId { get; set; } // null for web orders

      //  // NEW: accounting totals
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; }     // sum of OrderItem.LineCost

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalProfit { get; set; }   // GrandTotal - TotalCost


        [Required, StringLength(20)]
        [RegularExpression("^(Pending|Fulfilled|Cancelled)$")]
        public string Status { get; set; } = OrderStatuses.Pending;

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        public bool StockReturnedOnCancel { get; set; } = false;
    }
}
