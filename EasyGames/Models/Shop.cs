using System.ComponentModel.DataAnnotations;

namespace EasyGames.Models
{
    public class Shop
    {
        public int ShopId { get; set; }

        [Required, StringLength(120)]
        [Display(Name = "Shop Name")]
        public string Name { get; set; }

        [Required, StringLength(500)]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Required]
        [Display(Name = "Proprietor")]
        public string ProprietorUserId { get; set; }

        // Navigation properties
        public ApplicationUser? Proprietor { get; set; }
        public ICollection<ShopStock> ShopStock { get; set; } = new List<ShopStock>();
    }
}