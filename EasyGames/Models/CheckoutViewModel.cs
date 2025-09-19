using System.ComponentModel.DataAnnotations;
//  holds the readonly list of cartitems the user is about to purchase and a subtotal precalculated on the server
//  captures shippingname and shippingaddress with validation attributes to enforce clean input
namespace EasyGames.Models
{
    public record CheckoutViewModel
    {
        public IReadOnlyList<CartItem> Items { get; init; } = Array.Empty<CartItem>();
        public decimal Subtotal { get; init; }

        [Required, StringLength(100)] public string ShippingName { get; set; } = "";
        [Required, StringLength(200)] public string ShippingAddress { get; set; } = "";
    }
}
