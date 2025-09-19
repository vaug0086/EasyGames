using System.Text;
using System.Text.Encodings.Web;
using EasyGames.Models;                       
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.Threading.Tasks;
//  backs the registerconfirmation.cshtml razor page, shown right after a user signs up
//  renders a clickable confirmation for the purposes of url dev/testing 
//  uses usermanager.generateemailconfirmationtokenasync to create a token, encodes it with webencoders.base64urlencode
//  and injects it into a url.page() link that points to confirmemail.cshtml
//  ieamailsender is injected so that in production the token is emailed, not shown on screen
namespace EasyGames.Areas.Identity.Pages.Account
{
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public RegisterConfirmationModel(
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public string Email { get; set; }
        public bool DisplayConfirmAccountLink { get; set; }
        public string EmailConfirmationUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
        {
            if (email == null) return RedirectToPage("/Index");
            Email = email;

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound($"Unable to load user with email '{email}'.");

            //  If you want to display the link directly (dev only)

            if (DisplayConfirmAccountLink)
            {
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                EmailConfirmationUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = user.Id, code, returnUrl },
                    protocol: Request.Scheme);
            }

            return Page();
        }
    }
}
