using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyGames.Models
{
    public class CustomerProfile
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = default!;   // FK to Identity user

        [Column(TypeName = "decimal(18,2)")]
        public decimal LifetimeProfitContribution { get; set; } = 0m;

        public Tier CurrentTier { get; set; } = Tier.Bronze;
    }
}
