using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;



//  what this class does
//  implements iemailsender so identity can send confirmation and reset emails
//  instead of sending real email it logs the recipient subject and html body to the console via ilogger
//  this makes email confirmation flows testable during development without having to kill myself with smtp setup


namespace EasyGames.Services
{
    public class ConsoleEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            Console.WriteLine($"[EMAIL] To: {email}\nSubj: {subject}\n{htmlMessage}");
            return Task.CompletedTask;
        }
    }
}