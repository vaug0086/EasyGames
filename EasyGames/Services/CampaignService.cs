using EasyGames.Data;
using EasyGames.Models.Emailing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace EasyGames.Services
{
    public class CampaignService : ICampaignService
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _email; 

        public CampaignService(ApplicationDbContext db, IEmailSender email)
        { _db = db; _email = email; }

        public async Task<int> CreateCampaignAsync(string name, string subject, string bodyHtml, int groupId)
        {
            var group = await _db.CustomerGroups.Include(g => g.Members).FirstAsync(g => g.Id == groupId);

            var c = new EmailCampaign
            {
                Name = name,
                Subject = subject,
                BodyHtml = bodyHtml,
                GroupId = groupId,
                Status = CampaignStatus.Draft
            };

            var recipients = group.Members
                .Where(m => !m.Unsubscribed && !string.IsNullOrWhiteSpace(m.Email))
                .GroupBy(m => m.Email.Trim().ToLowerInvariant())
                .Select(g => new EmailCampaignRecipient
                {
                    Email = g.Key,
                    DisplayName = g.First().DisplayName
                }).ToList();

            c.Recipients = recipients;
            _db.EmailCampaigns.Add(c);
            await _db.SaveChangesAsync();
            return c.Id;
        }

        public async Task StartNowAsync(int campaignId)
        {
            var c = await _db.EmailCampaigns
                .Include(x => x.Recipients)
                .FirstAsync(x => x.Id == campaignId);

            c.Status = CampaignStatus.Sending;
            await _db.SaveChangesAsync();

            foreach (var r in c.Recipients.Where(x => x.Status == SendStatus.Queued))
            {
                try
                {
                    await _email.SendEmailAsync(r.Email, c.Subject, c.BodyHtml);
                    r.Status = SendStatus.Sent;
                    r.SentUtc = DateTime.UtcNow;
                    r.LastError = null;
                }
                catch (Exception ex)
                {
                    r.Status = SendStatus.Failed;
                    r.LastError = ex.Message;
                }
            }

            c.Status = c.Recipients.All(x => x.Status == SendStatus.Sent)
                ? CampaignStatus.Completed : CampaignStatus.Failed;

            await _db.SaveChangesAsync();
        }
    }
}
