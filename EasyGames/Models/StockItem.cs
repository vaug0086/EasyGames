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
        [Display(Name = "Buy Price")]
        public decimal BuyPrice { get; set; }

        [DataType(DataType.Currency)]
        [Precision(18, 2)]
        [Display(Name = "Sell Price")]
        public decimal SellPrice { get; set; }

        [Range(0, 100000)]
        public int Quantity { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        //  Given this assignment is not hosted and with blob storage, and is only for development at this stage,
        //  image upload has not been made functional. Instead the user must provide links to existing publicly
        //  available images. Long term this would be transitioned to a hosted blob storage like S3.
        [Url]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }
        //  A time stamp for the modified row...
        //  This prevents concurrency conflicts
        //  If the user updated the same item between the previous save and load
        //  the rowversion will not match and then it'll throw a concurrencyexception
        //  Source from here: https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/concurrency?view=aspnetcore-9.0
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
