using EasyGames.Data;
using EasyGames.Models.Emailing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin")]
public class EmailGroupsController : Controller
{
    private readonly ApplicationDbContext _db;
    public EmailGroupsController(ApplicationDbContext db) => _db = db;

    // EmailGroups
    public async Task<IActionResult> Index()
        => View(await _db.CustomerGroups
                         .Include(g => g.Members)
                         .OrderBy(g => g.Name)
                         .ToListAsync());

    // EmailGroups/Create
    [HttpGet]
    public IActionResult Create() => View(new CustomerGroup());

    // EmailGroups/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerGroup g)
    {
        if (!ModelState.IsValid) return View(g);

        _db.CustomerGroups.Add(g);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // EmailGroups/Manage
    [HttpGet]
    public async Task<IActionResult> Manage(int id)
    {
        var g = await _db.CustomerGroups
                         .Include(x => x.Members)
                         .FirstOrDefaultAsync(x => x.Id == id);

        return g is null ? NotFound() : View(g);
    }

    // EmailGroups/AddMember
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(int id, string email, string? name)
    {
        var group = await _db.CustomerGroups.FindAsync(id);
        if (group is null) return NotFound();

        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Email is required.";
            return RedirectToAction(nameof(Manage), new { id });
        }

        //  prevent duplicates
        var exists = await _db.CustomerGroupMembers
                              .AnyAsync(m => m.GroupId == id && m.Email == email);
        if (!exists)
        {
            _db.CustomerGroupMembers.Add(new CustomerGroupMember
            {
                GroupId = id,
                Email = email.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(name) ? null : name.Trim()
            });
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Manage), new { id });
    }
}
