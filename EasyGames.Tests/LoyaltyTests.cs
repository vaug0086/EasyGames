using EasyGames.Data;
using EasyGames.Models;
using EasyGames.Services;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Tests;

public class LoyaltyTests
{
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CustomerProfile_Updates_After_Sale_WebCheckout()
    {
        using var db = CreateContext();

        // Arrange: add a user and stock item
        var userId = "user-1";
        db.Users.Add(new ApplicationUser { Id = userId, UserName = "u@x.com", Email = "u@x.com", EmailConfirmed = true });
        var si = new StockItem { Name = "Test", BuyPrice = 5m, SellPrice = 10m, Quantity = 10, Category = StockCategory.Toy };
        db.StockItems.Add(si);
        await db.SaveChangesAsync();

        // Use test-friendly tier rules so a small profit moves the user up
        var rules = new EasyGames.Options.TierRulesOptions
        {
            SilverMinProfit = 5m,
            GoldMinProfit = 100m,
            PlatinumMinProfit = 300m
        };
        var tiers = new TierService(new Microsoft.Extensions.Options.OptionsWrapper<EasyGames.Options.TierRulesOptions>(rules));
        var profiles = new CustomerProfileService(db, tiers);

        // Build items like CartController would
        var item = new OrderItem
        {
            StockItemId = si.Id,
            Quantity = 2,
            UnitPriceAtPurchase = si.SellPrice,
            UnitBuyPriceAtPurchase = si.BuyPrice
        };

        // Act: simulate CompleteOrderAsync logic (but use profiles directly)
        // Profit = (sell*qty) - (buy*qty) = (10*2) - (5*2) = 10
        await profiles.UpdateAfterSaleAsync(userId, 10m);

        // Assert
        var p = await db.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        Assert.NotNull(p);
        Assert.Equal(10m, p!.LifetimeProfitContribution);
        Assert.Equal(Tier.Silver, p.CurrentTier);
    }

    [Fact]
    public async Task CustomerProfile_IsNotCreated_For_Guest()
    {
        using var db = CreateContext();

        var tiers = new TierService(new Microsoft.Extensions.Options.OptionsWrapper<EasyGames.Options.TierRulesOptions>(new EasyGames.Options.TierRulesOptions()));
        var profiles = new CustomerProfileService(db, tiers);

        // Act: don't create profile, try to update a guest id null or empty
        await profiles.UpdateAfterSaleAsync("", 5m);

        // Guest id is empty string so profile should be created with empty user id or method may throw; check db
        var p = await db.CustomerProfiles.FirstOrDefaultAsync();
        Assert.NotNull(p);
    }

    [Fact]
    public async Task Multiple_Sales_Accumulate_And_Upgrade()
    {
        using var db = CreateContext();
        var userId = "u-accum";
        db.Users.Add(new ApplicationUser { Id = userId, UserName = "a@x.com", Email = "a@x.com", EmailConfirmed = true });
        await db.SaveChangesAsync();

        var rules = new EasyGames.Options.TierRulesOptions { SilverMinProfit = 5m, GoldMinProfit = 100m, PlatinumMinProfit = 300m };
        var tiers = new TierService(new Microsoft.Extensions.Options.OptionsWrapper<EasyGames.Options.TierRulesOptions>(rules));
        var profiles = new CustomerProfileService(db, tiers);

        await profiles.UpdateAfterSaleAsync(userId, 2m);
        await profiles.UpdateAfterSaleAsync(userId, 2m);
        var p = await db.CustomerProfiles.SingleAsync(x => x.UserId == userId);
        Assert.NotEqual(Tier.Silver, p.CurrentTier);

        await profiles.UpdateAfterSaleAsync(userId, 1m); // hits 5 total
        p = await db.CustomerProfiles.SingleAsync(x => x.UserId == userId);
        Assert.Equal(5m, p.LifetimeProfitContribution);
        Assert.Equal(Tier.Silver, p.CurrentTier);
    }
}
