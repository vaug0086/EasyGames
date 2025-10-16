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
    private readonly ISalesService _sales;
    private readonly ICustomerProfileService _profiles;

    public CartController(ICartService cart, ApplicationDbContext db, ISalesService sales, ICustomerProfileService profiles)
    { _cart = cart; _db = db; _sales = sales; _profiles = profiles; }

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

    //  Allow anonymous access to the GET so we can show a friendly message instead of an error
    public IActionResult Checkout()
    {
        //  If user is not authenticated, send them back to the cart with an info message
        if (!(User?.Identity?.IsAuthenticated ?? false))
        {
            TempData["AlertInfo"] = "You must be logged in to checkout.";
            return RedirectToAction(nameof(Index));
        }

        var items = _cart.GetItems();
        var vm = new CheckoutViewModel
        {
            Items = items,
            Subtotal = items.Sum(i => i.LineTotal),
            ShippingName = User.Identity?.Name ?? ""
        };
        return View(vm);
    }
    //  Below is a modified function for the checkout. 
    //  The method processes the purchase, creates an order, decrements the
    //  stock quantity and then clears the users cart.
    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel vm)
    {
        var items = _cart.GetItems();
        if (!items.Any())
        {
            ModelState.AddModelError("", "Your cart is empty.");
            return View(vm with { Items = items, Subtotal = 0 });
        }
        //  Grab item from the database using distinct because I have no idea
        //  what shape the database is in rn.
        var ids = items.Select(i => i.StockItemId).Distinct().ToList();

        var stockItems = await _db.StockItems
            .Where(s => ids.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);

        foreach (var it in items)
        {
            if (!stockItems.TryGetValue(it.StockItemId, out var s))
            {
                ModelState.AddModelError("", $"Item '{it.Name}' no longer exists.");
                return View(vm with { Items = items, Subtotal = items.Sum(i => i.LineTotal) });
            }
            it.UnitPrice = s.SellPrice;
        }

        if (!ModelState.IsValid)
            return View(vm with { Items = items, Subtotal = items.Sum(i => i.LineTotal) });
        /* I don't think we actually need this. Becuse, don't we need to allow the stock to place
         * even if there isn't enough? Who I've commmented out. Uncomment it or modify it depending
         * on whatever you decided. I can't remember if it's POS stock that can go negative or what.
        foreach (var line in items)
        {
            var s = stockItems[line.StockItemId];
            if (s.Quantity < line.Quantity)
            {
                ModelState.AddModelError("",
                    $"Not enough stock for {s.Name}. Requested {line.Quantity}, available {s.Quantity}.");
                return View(vm with { Items = items, Subtotal = items.Sum(i => i.LineTotal) });
            }
        }
        */

        await using var tx = await _db.Database.BeginTransactionAsync();

        var order = new Order
        {
            UserId = User.GetUserId()!,
            CreatedUtc = DateTime.UtcNow,
            ShippingName = vm.ShippingName,
            ShippingAddress = vm.ShippingAddress,
            Subtotal = items.Sum(i => i.LineTotal),
            GrandTotal = items.Sum(i => i.LineTotal)
        };
        //  Decrement the stock quantites
        foreach (var it in items)
        {
            var s = stockItems[it.StockItemId];
            s.Quantity -= it.Quantity; // Stock minus purchases

            order.Items.Add(new OrderItem
            {
                StockItemId = it.StockItemId,
                Quantity = it.Quantity,
                UnitPriceAtPurchase = it.UnitPrice,
                UnitBuyPriceAtPurchase = s.BuyPrice // record buy price for profit calculation
            });
        }

        // Compute totals for cost/profit so we can update loyalty
        order.TotalCost = order.Items.Sum(i => i.UnitBuyPriceAtPurchase * i.Quantity);
        order.TotalProfit = order.GrandTotal - order.TotalCost;

        _db.Orders.Add(order);

        try
        {
            //  Try to commit, throw exception if race condition
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            //  Race condition. Thanks database subject!
            //  This prevents the user modifying stock between load and save
            await tx.RollbackAsync();
            TempData["AlertDanger"] = "Stock levels changed during checkout. Please review your cart.";
            return RedirectToAction(nameof(Checkout));
        }

        //  Update customer profile loyalty now that order is persisted
        if (!string.IsNullOrWhiteSpace(order.UserId))
        {
            //  User is authenticated because this endpoint requires
            await _profiles.UpdateAfterSaleAsync(order.UserId, order.TotalProfit);
        }

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