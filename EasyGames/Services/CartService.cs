using System.Security.Claims;
using System.Text.Json;
using EasyGames.Data;
using EasyGames.Models;
using EasyGames.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

//  provides add/update/remove/clear and summary ops over a per-visitor cart
//  stores cart lines (cartitem) in asp.net core session as json for quick, server-side state key design points


public class CartService : ICartService
{
    private const string SessionPrefix = "CART_V1_";
    private const string AnonCartCookie = "anon_cart_id";

    private readonly IHttpContextAccessor _http;
    private readonly ApplicationDbContext _db;

    public CartService(IHttpContextAccessor http, ApplicationDbContext db)
    {
        _http = http;
        _db = db;
    }

    public void Add(int stockItemId, int qty = 1)
    {
        qty = Math.Max(1, qty);

        var items = Load();
        var existing = items.FirstOrDefault(i => i.StockItemId == stockItemId);
        if (existing is null)
        {
            var s = _db.StockItems
                       .AsNoTracking()
                       .FirstOrDefault(x => x.Id == stockItemId)
                    ?? throw new InvalidOperationException("Item not found");

            items.Add(new CartItem
            {
                StockItemId = s.Id,
                Name = s.Name,
                UnitPrice = s.Price,
                Quantity = qty
            });
        }
        else
        {
            existing.Quantity += qty;
        }

        Save(items);
    }

    public void UpdateQty(int stockItemId, int qty)
    {
        var items = Load();
        var it = items.FirstOrDefault(i => i.StockItemId == stockItemId);
        if (it is null) return;

        if (qty <= 0) items.Remove(it);
        else it.Quantity = qty;

        Save(items);
    }

    public void Remove(int stockItemId)
    {
        var items = Load();
        items.RemoveAll(i => i.StockItemId == stockItemId);
        Save(items);
    }

    public IReadOnlyList<CartItem> GetItems() => Load();

    public int GetCount() => Load().Sum(i => i.Quantity);

    public decimal GetSubtotal() => Load().Sum(i => i.LineTotal);

    public void Clear() => Save(new List<CartItem>());

    //  Helper functions

    private List<CartItem> Load()
    {
        var key = GetCartSessionKey();
        var str = _http.HttpContext!.Session.GetString(key);
        return string.IsNullOrEmpty(str)
            ? new List<CartItem>()
            : (JsonSerializer.Deserialize<List<CartItem>>(str) ?? new List<CartItem>());
    }

    private void Save(List<CartItem> items)
    {
        var key = GetCartSessionKey();
        _http.HttpContext!.Session.SetString(key, JsonSerializer.Serialize(items));
    }

    private string GetCartSessionKey()
    {
        var ctx = _http.HttpContext ?? throw new InvalidOperationException("No HttpContext");
        var user = ctx.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? throw new InvalidOperationException("Missing user id claim");
            return SessionPrefix + "USER_" + userId;
        }

        var anonId = EnsureAnonCookie(ctx);
        return SessionPrefix + "ANON_" + anonId;
    }

    private static string EnsureAnonCookie(HttpContext ctx)
    {
        if (ctx.Request.Cookies.TryGetValue(AnonCartCookie, out var existing) && !string.IsNullOrEmpty(existing))
            return existing;

        var value = Guid.NewGuid().ToString("N");
        ctx.Response.Cookies.Append(
            AnonCartCookie,
            value,
            new CookieOptions
            {
                IsEssential = true,     // survives cookie consent
                HttpOnly = true,
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                SameSite = SameSiteMode.Lax,
                Secure = ctx.Request.IsHttps
            });

        return value;
        
    }
    //  Below function does
    //  when a visitor logs in, this routine pulls "CART_V1_ANON_{anonId}" from session (if present),
    //  loads the user’s "CART_V1_USER_{userId}", and merges line quantities by stockitemid.
    //  after writing the merged user cart, it removes the anon cart key to prevent duplicates.
    public void MergeAnonCartIntoCurrentUser()
    {
        var ctx = _http.HttpContext ?? throw new InvalidOperationException("No HttpContext");
        var user = ctx.User;

        if (user?.Identity?.IsAuthenticated != true) return;

        if (!ctx.Request.Cookies.TryGetValue("anon_cart_id", out var anonId) || string.IsNullOrEmpty(anonId))
            return;

        var anonKey = $"CART_V1_ANON_{anonId}";

        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return;

        var userKey = $"CART_V1_USER_{userId}";

        var anonJson = ctx.Session.GetString(anonKey);
        if (string.IsNullOrEmpty(anonJson)) return;

        var userJson = ctx.Session.GetString(userKey);

        var anonItems = JsonSerializer.Deserialize<List<CartItem>>(anonJson) ?? new();
        var userItems = string.IsNullOrEmpty(userJson)
            ? new List<CartItem>()
            : (JsonSerializer.Deserialize<List<CartItem>>(userJson) ?? new());

        foreach (var a in anonItems)
        {
            var u = userItems.FirstOrDefault(x => x.StockItemId == a.StockItemId);
            if (u == null) userItems.Add(a);
            else u.Quantity += a.Quantity;
        }

        ctx.Session.SetString(userKey, JsonSerializer.Serialize(userItems));
        ctx.Session.Remove(anonKey);
    }
}
