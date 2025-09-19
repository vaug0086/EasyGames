using EasyGames.Data;
using EasyGames.Extensions;
using EasyGames.Models;
using EasyGames.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

//  what this does
//  uses DI to get ICartService and ApplicationDbContext 
//  provides add/update/remove actions for the cart and a two-step checkout
public class CartController : Controller
{
    private readonly ICartService _cart;
    private readonly ApplicationDbContext _db;

    public CartController(ICartService cart, ApplicationDbContext db)
    { _cart = cart; _db = db; }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Add(int id, int qty = 1, string? returnUrl = null)
    {
        if (qty < 1) qty = 1;

        // actually add to cart (session-backed)
        _cart.Add(id, qty);

        TempData["CartMessage"] = "Added to cart.";

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Catalogue");
    }

    public IActionResult Index() => View(_cart.GetItems());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Update(int id, int qty)
    {
        _cart.UpdateQty(id, qty);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Remove(int id)
    {
        _cart.Remove(id);
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public IActionResult Checkout()
    {
        var items = _cart.GetItems();
        var vm = new CheckoutViewModel
        {
            Items = items,
            Subtotal = items.Sum(i => i.LineTotal),
            ShippingName = User.Identity?.Name ?? ""
        };
        return View(vm);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel vm)
    {
        var items = _cart.GetItems();
        if (!items.Any())
        {
            ModelState.AddModelError("", "Your cart is empty.");
            return View(vm with { Items = items, Subtotal = 0 });
        }

        var ids = items.Select(i => i.StockItemId).ToList();
        var current = await _db.StockItems.AsNoTracking()
                          .Where(s => ids.Contains(s.Id)).ToListAsync();
        foreach (var it in items)
        {
            var s = current.FirstOrDefault(c => c.Id == it.StockItemId);
            if (s is null)
            {
                ModelState.AddModelError("", $"Item '{it.Name}' no longer exists.");
                return View(vm with { Items = items, Subtotal = items.Sum(i => i.LineTotal) });
            }
            it.UnitPrice = s.Price;
        }

        if (!ModelState.IsValid)
            return View(vm with { Items = items, Subtotal = items.Sum(i => i.LineTotal) });

        var order = new Order
        {
            UserId = User.GetUserId()!,
            CreatedUtc = DateTime.UtcNow,
            ShippingName = vm.ShippingName,
            ShippingAddress = vm.ShippingAddress,
            Subtotal = items.Sum(i => i.LineTotal),
            GrandTotal = items.Sum(i => i.LineTotal)
        };
        foreach (var it in items)
            order.Items.Add(new OrderItem
            {
                StockItemId = it.StockItemId,
                Quantity = it.Quantity,
                UnitPriceAtPurchase = it.UnitPrice
            });

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        _cart.Clear();

        return RedirectToAction(nameof(Thanks), new { id = order.Id });
    }

    [Authorize]
    public async Task<IActionResult> Thanks(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.StockItem)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == User.GetUserId());
        if (order is null) return NotFound();
        return View(order);
    }
}