using EasyGames.Data;
using EasyGames.Models.Emailing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


[AllowAnonymous]
[Route("campaigns")]
public class PublicCampaignsController : Controller
{
    private readonly ApplicationDbContext _db;
    public PublicCampaignsController(ApplicationDbContext db) { _db = db; }

    [HttpGet("")]
    public async Task<IActionResult> Index()
        => View(await _db.EmailCampaigns
            .Include(c => c.Group)
            .Where(c => c.IsPublic)
            .OrderByDescending(c => c.PublishedUtc ?? c.ScheduledUtc ?? c.CreatedUtc)
            .ToListAsync());

    [HttpGet("{id:int}/{slug?}")]
    public async Task<IActionResult> Details(int id)
    {
        var c = await _db.EmailCampaigns.Include(x => x.Group)
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsPublic);
        return c is null ? NotFound() : View(c);
    }
}