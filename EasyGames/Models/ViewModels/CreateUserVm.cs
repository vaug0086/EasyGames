
//  email required with emailaddress attribute for validation
//  password required with dataype.password and stringlength enforcing min 6 chars and max 100
//  isadmin boolean flag lets the admin assign the new account to the admin role on creation
using System.ComponentModel.DataAnnotations;

namespace EasyGames.Models.Admin
{
    public class CreateUserVm
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = "";

        public bool IsAdmin { get; set; }
    }
}
