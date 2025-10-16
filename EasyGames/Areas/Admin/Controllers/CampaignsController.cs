using EasyGames.Data;
using EasyGames.Models.Emailing;
using EasyGames.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CampaignsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ICampaignService _svc;

        public CampaignsController(ApplicationDbContext db, ICampaignService svc)
        {
            _db = db;
            _svc = svc;
        }

        // Admin/Campaigns
        public async Task<IActionResult> Index()
            => View(await _db.EmailCampaigns
                .Include(c => c.Group)
                .OrderByDescending(c => c.PublishedUtc ?? c.ScheduledUtc ?? c.CreatedUtc)
                .ToListAsync());

        // Admin/Campaigns/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Groups = await _db.CustomerGroups.OrderBy(g => g.Name).ToListAsync();
            return View();
        }

        // Admin/Campaigns/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string name,
            string subject,
            int groupId,
            string details,
            string? imageUrl,
            bool isPublic = false)
        {
            name = name?.Trim() ?? "";
            subject = subject?.Trim() ?? "";
            details = details?.Trim() ?? "";
            imageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(subject) ||
                string.IsNullOrWhiteSpace(details))
            {
                ModelState.AddModelError("", "Name, Subject and Details are required.");
            }

            if (imageUrl is not null && !IsValidAbsoluteHttpUrl(imageUrl))
            {
                ModelState.AddModelError("imageUrl", "Image URL must be an absolute http/https URL.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Groups = await _db.CustomerGroups.OrderBy(g => g.Name).ToListAsync();
                return View();
            }

            // Create campaign (plain-text details)
            var id = await _svc.CreateCampaignAsync(name, subject, null, groupId);

            var c = await _db.EmailCampaigns.FindAsync(id);
            if (c != null)
            {
                c.Details = details;
                c.ImageUrl = imageUrl;

                if (isPublic)
                {
                    c.IsPublic = true;
                    c.PublishedUtc ??= DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        //Admin/Campaigns/Edit
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var c = await _db.EmailCampaigns.FirstOrDefaultAsync(x => x.Id == id);
            if (c is null) return NotFound();

            ViewBag.Groups = await _db.CustomerGroups.OrderBy(g => g.Name).ToListAsync();
            return View(c);
        }

        // Admin/Campaigns/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            string name,
            string subject,
            int groupId,
            string details,
            string? imageUrl,
            bool removeImage = false)
        {
            var c = await _db.EmailCampaigns.FindAsync(id);
            if (c is null) return NotFound();

            name = name?.Trim() ?? "";
            subject = subject?.Trim() ?? "";
            details = details?.Trim() ?? "";
            imageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(subject) ||
                string.IsNullOrWhiteSpace(details))
            {
                ModelState.AddModelError("", "Name, Subject and Details are required.");
            }

            if (!removeImage && imageUrl is not null && !IsValidAbsoluteHttpUrl(imageUrl))
            {
                ModelState.AddModelError("imageUrl", "Image URL must be an absolute http/https URL.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Groups = await _db.CustomerGroups.OrderBy(g => g.Name).ToListAsync();
                return View(c);
            }

            c.Name = name;
            c.Subject = subject;
            c.GroupId = groupId;
            c.Details = details;
            c.ImageUrl = removeImage ? null : imageUrl;

            await _db.SaveChangesAsync();
            TempData["AlertSuccess"] = "Campaign updated.";
            return RedirectToAction(nameof(Details), new { id = c.Id });
        }

        // Admin/Campaigns/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.EmailCampaigns.FindAsync(id);
            if (c is null) return NotFound();

            _db.EmailCampaigns.Remove(c);
            await _db.SaveChangesAsync();
            TempData["AlertSuccess"] = "Campaign deleted.";
            return RedirectToAction(nameof(Index));
        }

        // Admin/Campaigns/Details
        public async Task<IActionResult> Details(int id)
        {
            var c = await _db.EmailCampaigns
                .Include(x => x.Group)
                .Include(x => x.Recipients)
                .FirstOrDefaultAsync(x => x.Id == id);

            return c is null ? NotFound() : View(c);
        }

        // Admin/Campaigns/StartNow
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartNow(int id)
        {
            await _svc.StartNowAsync(id);
            TempData["AlertSuccess"] = "Campaign started.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Admin/Campaigns/Publish/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            var c = await _db.EmailCampaigns.FindAsync(id);
            if (c is null) return NotFound();

            c.IsPublic = true;
            c.PublishedUtc ??= DateTime.UtcNow;
            if (c.Status == CampaignStatus.Draft)
                c.Status = CampaignStatus.Completed;

            await _db.SaveChangesAsync();
            TempData["Ok"] = "Campaign published.";
            return RedirectToAction(nameof(Details), new { id });
        }

        //Admin/Campaigns/Unpublish
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unpublish(int id)
        {
            var c = await _db.EmailCampaigns.FindAsync(id);
            if (c is null) return NotFound();

            c.IsPublic = false;
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Campaign unpublished.";
            return RedirectToAction(nameof(Details), new { id });
        }

       
        private static bool IsValidAbsoluteHttpUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }
    }
}
