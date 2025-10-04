using EasyGames.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
//  seed.cs populate database with starter data and admin account
//  provides a seedingtime method that runs at startup with a flag (--seed or --deseed) to populate or clear data

namespace EasyGames.Data
{
    public static class Seed
    {
        public static void SeedingTime(string flag, IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Database.Migrate();

            switch (flag)
            {
                case "--seed":
                    seeding(db);
                    SeedAdmin(serviceProvider);
                    break;
                case "--deseed":
                    deseeding(db);
                    break;
            }
        }

        private static void seeding(ApplicationDbContext dbinstance)
        {
            if (dbinstance.StockItems.Any()) return;

            // this was added from Frank's original project.
            // made up by ChatGPT, images manually downloaded from online
            dbinstance.StockItems.AddRange(
                // Books
                new StockItem { Name = "1984", Category = StockCategory.Book, BuyPrice = 10.99m, SellPrice = 14.99m, Quantity = 15, ImageUrl = "/images/seeds/book-1984.jpg" },
                new StockItem { Name = "The Alchemist", Category = StockCategory.Book, BuyPrice = 9.99m, SellPrice = 13.99m, Quantity = 12, ImageUrl = "/images/seeds/book-alchemist.jpg" },
                new StockItem { Name = "Brave New World", Category = StockCategory.Book, BuyPrice = 11.99m, SellPrice = 15.99m, Quantity = 10, ImageUrl = "/images/seeds/book-brave-new-world.jpg" },
                new StockItem { Name = "The Catcher in the Rye", Category = StockCategory.Book, BuyPrice = 12.99m, SellPrice = 16.99m, Quantity = 8, ImageUrl = "/images/seeds/book-catcher-rye.jpg" },
                new StockItem { Name = "The Great Gatsby", Category = StockCategory.Book, BuyPrice = 8.99m, SellPrice = 12.99m, Quantity = 20, ImageUrl = "/images/seeds/book-great-gatsby.jpg" },
                new StockItem { Name = "The Hobbit", Category = StockCategory.Book, BuyPrice = 13.99m, SellPrice = 18.99m, Quantity = 14, ImageUrl = "/images/seeds/book-hobbit.jpg" },
                new StockItem { Name = "The Lord of the Rings", Category = StockCategory.Book, BuyPrice = 18.99m, SellPrice = 24.99m, Quantity = 7, ImageUrl = "/images/seeds/book-lotr.jpg" },
                new StockItem { Name = "Moby Dick", Category = StockCategory.Book, BuyPrice = 13.99m, SellPrice = 17.99m, Quantity = 9, ImageUrl = "/images/seeds/book-moby-dick.jpg" },
                new StockItem { Name = "To Kill a Mockingbird", Category = StockCategory.Book, BuyPrice = 11.99m, SellPrice = 15.99m, Quantity = 11, ImageUrl = "/images/seeds/book-mockingbird.jpg" },
                new StockItem { Name = "Pride and Prejudice", Category = StockCategory.Book, BuyPrice = 10.99m, SellPrice = 14.99m, Quantity = 13, ImageUrl = "/images/seeds/book-pride-prejudice.jpg" },

                // Games
                new StockItem { Name = "Animal Crossing: New Horizons", Category = StockCategory.Game, BuyPrice = 35.99m, SellPrice = 49.99m, Quantity = 6, ImageUrl = "/images/seeds/game-animal-crossing.jpg" },
                new StockItem { Name = "Elden Ring", Category = StockCategory.Game, BuyPrice = 42.99m, SellPrice = 59.99m, Quantity = 8, ImageUrl = "/images/seeds/game-elden-ring.jpg" },
                new StockItem { Name = "Fortnite", Category = StockCategory.Game, BuyPrice = 7.23m, SellPrice = 10.23m, Quantity = 17, ImageUrl = "/images/seeds/game-fortnite.jpeg" },
                new StockItem { Name = "God of War", Category = StockCategory.Game, BuyPrice = 27.99m, SellPrice = 39.99m, Quantity = 12, ImageUrl = "/images/seeds/game-god-of-war.jpg" },
                new StockItem { Name = "Halo Infinite", Category = StockCategory.Game, BuyPrice = 42.99m, SellPrice = 59.99m, Quantity = 10, ImageUrl = "/images/seeds/game-halo-infinite.png" },
                new StockItem { Name = "Super Mario Odyssey", Category = StockCategory.Game, BuyPrice = 35.99m, SellPrice = 49.99m, Quantity = 15, ImageUrl = "/images/seeds/game-mario-odyssey.jpg" },
                new StockItem { Name = "Minecraft", Category = StockCategory.Game, BuyPrice = 20.99m, SellPrice = 29.99m, Quantity = 25, ImageUrl = "/images/seeds/game-minecraft.jpg" },
                new StockItem { Name = "Stardew Valley", Category = StockCategory.Game, BuyPrice = 10.99m, SellPrice = 14.99m, Quantity = 20, ImageUrl = "/images/seeds/game-stardew.png" },
                new StockItem { Name = "The Witcher 3: Wild Hunt", Category = StockCategory.Game, BuyPrice = 27.99m, SellPrice = 39.99m, Quantity = 7, ImageUrl = "/images/seeds/game-witcher3.jpg" },
                new StockItem { Name = "The Legend of Zelda: Breath of the Wild", Category = StockCategory.Game, BuyPrice = 42.99m, SellPrice = 59.99m, Quantity = 9, ImageUrl = "/images/seeds/game-zelda.jpg" },

                // Toys
                new StockItem { Name = "Barbie Dreamhouse Doll", Category = StockCategory.Toy, BuyPrice = 17.99m, SellPrice = 24.99m, Quantity = 18, ImageUrl = "/images/seeds/toy-barbie.jpg" },
                new StockItem { Name = "Hot Wheels Car Set", Category = StockCategory.Toy, BuyPrice = 13.99m, SellPrice = 19.99m, Quantity = 22, ImageUrl = "/images/seeds/toy-hotwheels.jpg" },
                new StockItem { Name = "Jenga Classic Game", Category = StockCategory.Toy, BuyPrice = 8.99m, SellPrice = 12.99m, Quantity = 16, ImageUrl = "/images/seeds/toy-jenga.jpg" },
                new StockItem { Name = "LEGO Creator 3-in-1", Category = StockCategory.Toy, BuyPrice = 55.99m, SellPrice = 79.99m, Quantity = 8, ImageUrl = "/images/seeds/toy-lego.png" },
                new StockItem { Name = "Nerf Elite Blaster", Category = StockCategory.Toy, BuyPrice = 24.99m, SellPrice = 34.99m, Quantity = 12, ImageUrl = "/images/seeds/toy-nerf.jpg" },
                new StockItem { Name = "Play-Doh Modeling Compound", Category = StockCategory.Toy, BuyPrice = 6.99m, SellPrice = 9.99m, Quantity = 30, ImageUrl = "/images/seeds/toy-playdoh.webp" },
                new StockItem { Name = "RC Stunt Car", Category = StockCategory.Toy, BuyPrice = 32.99m, SellPrice = 45.99m, Quantity = 6, ImageUrl = "/images/seeds/toy-rc-car.jpeg" },
                new StockItem { Name = "Rubik's Cube 3x3", Category = StockCategory.Toy, BuyPrice = 10.99m, SellPrice = 14.99m, Quantity = 25, ImageUrl = "/images/seeds/toy-rubiks-cube.jpeg" },
                new StockItem { Name = "Classic Teddy Bear", Category = StockCategory.Toy, BuyPrice = 21.99m, SellPrice = 29.99m, Quantity = 14, ImageUrl = "/images/seeds/toy-teddy.jpg" },
                new StockItem { Name = "UNO Card Game", Category = StockCategory.Toy, BuyPrice = 5.99m, SellPrice = 7.99m, Quantity = 40, ImageUrl = "/images/seeds/toy-uno.jpg" }
            );
            dbinstance.SaveChanges();
        }

        private static void deseeding(ApplicationDbContext dbinstance)
        {
            if (!dbinstance.StockItems.Any()) return;
            dbinstance.StockItems.ExecuteDelete();
            dbinstance.SaveChanges();
        }

        private static void SeedAdmin(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // NOTE FROM FRANK - code written with the help of Claude.
            // Edited over many prompts but the main prompt for scaffolding was:
            // "I want to create 3 clear test users - admin, proprietor, user - in the seed file. Then, I want to transfer some stock across to the stores randomly and have stock in the main stock holder. Should be like a real company after the seed file. "
            const string adminRole = "Admin";
            const string proprietorRole = "Proprietor";
            const string adminEmail = "admin@easygames.com";
            const string adminPassword = "Admin123!";

            // Create Admin role
            if (!roleManager.RoleExistsAsync(adminRole).Result)
            {
                roleManager.CreateAsync(new IdentityRole(adminRole)).Wait();
            }

            // Create Proprietor role
            if (!roleManager.RoleExistsAsync(proprietorRole).Result)
            {
                roleManager.CreateAsync(new IdentityRole(proprietorRole)).Wait();
            }
            var adminUser = userManager.FindByEmailAsync(adminEmail).Result;
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                var result = userManager.CreateAsync(adminUser, adminPassword).Result;
                if (!result.Succeeded)
                {
                    throw new Exception("Failed to create admin user: " +
                                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            if (!userManager.IsInRoleAsync(adminUser, adminRole).Result)
            {
                userManager.AddToRoleAsync(adminUser, adminRole).Wait();
            }

            // Create test proprietor user
            const string proprietorEmail = "proprietor@easygames.com";
            const string proprietorPassword = "Proprietor123!";

            var proprietorUser = userManager.FindByEmailAsync(proprietorEmail).Result;
            if (proprietorUser == null)
            {
                proprietorUser = new ApplicationUser
                {
                    UserName = proprietorEmail,
                    Email = proprietorEmail,
                    EmailConfirmed = true,
                    FullName = "Test Proprietor"
                };
                var result = userManager.CreateAsync(proprietorUser, proprietorPassword).Result;
                if (!result.Succeeded)
                {
                    throw new Exception("Failed to create proprietor user: " +
                                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            if (!userManager.IsInRoleAsync(proprietorUser, proprietorRole).Result)
            {
                userManager.AddToRoleAsync(proprietorUser, proprietorRole).Wait();
            }

            // Create regular user
            const string userEmail = "user@easygames.com";
            const string userPassword = "User123!";

            var regularUser = userManager.FindByEmailAsync(userEmail).Result;
            if (regularUser == null)
            {
                regularUser = new ApplicationUser
                {
                    UserName = userEmail,
                    Email = userEmail,
                    EmailConfirmed = true,
                    FullName = "Test User"
                };
                var result = userManager.CreateAsync(regularUser, userPassword).Result;
                if (!result.Succeeded)
                {
                    throw new Exception("Failed to create regular user: " +
                                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // Create shop and stock data
            SeedShopData(serviceProvider);
        }

        private static void SeedShopData(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Only seed shop if it doesn't exist
            if (db.Shops.Any()) return;

            var proprietorUser = userManager.FindByEmailAsync("proprietor@easygames.com").Result;
            if (proprietorUser == null) return;

            // Create test shop
            var testShop = new Shop
            {
                Name = "Game Corner Shop",
                Address = "123 Main Street, Darwin NT 0800",
                ProprietorUserId = proprietorUser.Id
            };

            db.Shops.Add(testShop);
            db.SaveChanges();

            // Add some stock items to the shop
            var stockItems = db.StockItems.Take(10).ToList(); // Get first 10 items from seed data

            foreach (var item in stockItems)
            {
                // Calculate safe transfer amount (max 50% of available stock, minimum 1, maximum 15)
                var maxTransfer = Math.Min(item.Quantity / 2, 15);
                var transferAmount = maxTransfer > 0 ? Random.Shared.Next(1, Math.Max(2, maxTransfer + 1)) : 0;

                if (transferAmount > 0)
                {
                    var shopStock = new ShopStock
                    {
                        ShopId = testShop.ShopId,
                        StockItemId = item.Id,
                        QtyOnHand = transferAmount,
                        LowStockThreshold = 5,
                        InheritedBuyPrice = item.BuyPrice,
                        InheritedSellPrice = item.SellPrice
                    };
                    db.ShopStock.Add(shopStock);

                    // Reduce main inventory by the amount transferred to shop
                    item.Quantity -= transferAmount;
                }
            }

            db.SaveChanges();
        }
    }
}
