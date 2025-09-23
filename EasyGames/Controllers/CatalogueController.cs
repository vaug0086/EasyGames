/* 
    This a public catalogue endpoint for browsing stock. It does not provide editing functionality.
    It is fundementally built upon the idea of read only queries so AsNoTracking() is used.
    What this does act as an endpoint for browsing stock so Users can search and filter by category. 
    AllowAnonymous is used because it is public facing. Any CRUD action is locked behind Authorise.
    
    Listen, I'm gonna level with you I'm really struggling to write these notes. I understand the purpose
    of this subject but when you've done C# before and for the past 3 semesters (And in your TAFE subjects) have made an MVC, 
    it really gets hard to know what is obvious and what's something you need to explain. At my other univeristy, I am doing almost exactly the same
    thing as this subject but in PHP. So, I hope you'll go easy on me if these notes seem repetive or maybe I missed detail. 
    There's only so many times you can make an MVC in [PROGRAMMING LANGUAGE HERE] before it all starts to sound the same.
    I know we need to cite sources too and so I've just been reading all the documentation for C# to refresh my memory.
    I'll cite what I use in the notes but I'm probably gonna over cite, again, I just don't know what's obvious anymore.
*/

/*  References for this document
 *  https://learn.microsoft.com/ef/core/querying/tracking
 *  https://learn.microsoft.com/ef/core/miscellaneous/async
 *  https://learn.microsoft.com/aspnet/core/data/ef-mvc/sort-filter-page
 *  https://learn.microsoft.com/aspnet/core/mvc/views/tag-helpers/built-in/anchor-tag-helper
 *  https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.authorization.allowanonymousattribute
 *  https://learn.microsoft.com/dotnet/csharp/language-reference/operators/switch-expression
*/


using System.Linq;                     
using System.Threading.Tasks;          //   Async stuff. Needed for webapps.
using EasyGames.Data;
using EasyGames.Models;
using EasyGames.Models.ViewModels;    
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Controllers
{
    [AllowAnonymous] // Let's unauthorised users hit the controller.
    public class CatalogueController : Controller
    {
        //  This contorller's appliationcontext is read only. Only for this controller.
        private readonly ApplicationDbContext _context;
        public CatalogueController(ApplicationDbContext context) => _context = context;
        //  Construct dependency inhjection. CatalogueController is constructed, every time CatalogueController.
        //  is constructed we inject ApplicationDbContext into the construction.

        //  Async to prevent blocking the database. Multiple queries at a time. Always use Async for webapps.
        //  Task is used for asynchronous applications
        public async Task<IActionResult> Index(
            //  Create the basis for the index page. Unless specified start at page one, page size 12.
            //  Search, Category and sort are all allowed to be null hence the ?
            string? search,                 
            StockCategory? category,        
            string? sort,                  
            int page = 1,                   
            int pageSize = 12)              
        {
            //  Apparently when you make requests to the database the EF tracks them. Makes sense for data handling where we are
            //  updating but not much use if we are just reading. So, we use AsNoTracking which tells the EF "Dont bother tracking this"
            //  Fetch the items but no change tracking
            var q = _context.StockItems.AsNoTracking();

            //  Search by name or desc (case-insensitive pattern)
            if (!string.IsNullOrWhiteSpace(search))
                // If the search is not empty
            {
                var pattern = $"%{search}%";
                //  This is very similar to how Django handles this. 
                //  Instance of db as an object. Call functions on that object using LINQ 
                q = q.Where(s =>
                    EF.Functions.Like(s.Name, pattern) ||
                    (s.Description != null && EF.Functions.Like(s.Description, pattern)));
            }

            //  Filter by category
            if (category.HasValue)
            {
                q = q.Where(s => s.Category == category);
            }

            //  Sort
            /* This is an old style swithc case that C# apparently has a new way of doing things.
            switch (sort)
                {
                    case "name_desc":
                        q = q.OrderByDescending(s => s.Name);
                        break;
                    case "price_asc":
                        q = q.OrderBy(s => s.SellPrice).ThenBy(s => s.Name);
                        break;
                    case "price_desc":
                        q = q.OrderByDescending(s => s.SellPrice).ThenBy(s => s.Name);
                        break;
                    default:
                        q = q.OrderBy(s => s.Name);
                        break;
                }
            */
            q = sort switch
            {
                "name_desc" => q.OrderByDescending(s => s.Name),
                "price_asc" => q.OrderBy(s => s.SellPrice).ThenBy(s => s.Name),
                "price_desc" => q.OrderByDescending(s => s.SellPrice).ThenBy(s => s.Name),
                _ => q.OrderBy(s => s.Name)
            };

            //  Calculate the total number of items and pages after filtering is applied.
            //  This allows us to smooth out UI and know what to display. Total and items are passed to model.
            var total = await q.CountAsync();
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 12;

            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            //  Return a CatalogueFilterViewModel with attributes in it.
            var vm = new CatalogueFilterViewModel
            {
                Search = search,
                Category = category,
                Sort = sort,
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Items = items
            };

            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            //  The stock item we return is the first one that matches the ID. No tracking again because we don't need
            //  to make any changes. Return null if not found.
            var item = await _context.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            if (item == null) return NotFound();

            // Get shop stock information for this item
            var shopStock = await _context.ShopStock
                .AsNoTracking() // just reading, don't track changes
                .Include(ss => ss.Shop) // adds related Shop for each ShopStock record
                .Where(ss => ss.StockItemId == id && ss.QtyOnHand > 0) // filters only shops that have this stock item (i.e., >0)
                .Select(ss => new ShopStockViewModel // we use a ViewModel which is a much simpler entity with only the necessary info
                {
                    ShopName = ss.Shop!.Name,
                    ShopAddress = ss.Shop.Address,
                    QtyOnHand = ss.QtyOnHand,
                    IsLowStock = ss.IsLowStock
                })
                .OrderBy(ss => ss.ShopName) // alphabetical order
                .ToListAsync(); // converted to list and fetched async (no blocking)

            var viewModel = new CatalogueDetailsViewModel // chuck all this info into an even more overarching view model before passing it to the view
            {
                StockItem = item,
                ShopStock = shopStock
            };

            return View(viewModel);
        }
    }
}
