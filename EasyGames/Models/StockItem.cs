using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EasyGames.Models
{
    //  Create the base model for the stock
    //  uses id as primary key
    //  name required up to 120 chars with display attribute for label
    //  category is an enum (book toy game) so filtering is type safe
    //  price is a decimal with currency annotation and precision(18,2) for reliable money storage
    //  quantity validated with range to prevent negatives
    //  description optional up to 1000 chars
    //  imageurl optional validated as url for product photo
    public enum StockCategory { Book, Toy, Game}
    public class StockItem
    {

        public int Id { get; set; }

        [Required, StringLength(120)]
        [Display(Name = "Item Name")]
        public string Name { get; set; }
        [Required]
        public StockCategory Category { get; set; }

        [DataType(DataType.Currency)]
        [Precision(18, 2)]
        public decimal Price { get; set; }

        [Range(0, 100000)]
        public int Quantity { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Url]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }
    }
}
