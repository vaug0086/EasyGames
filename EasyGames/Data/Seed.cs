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

            dbinstance.StockItems.AddRange(
                // Books
                new StockItem { Name = "1984", Category = StockCategory.Book, Price = 14.99m, Quantity = 15, ImageUrl = "/images/seeds/book-1984.jpg" },
                new StockItem { Name = "The Alchemist", Category = StockCategory.Book, Price = 13.99m, Quantity = 12, ImageUrl = "/images/seeds/book-alchemist.jpg" },
                new StockItem { Name = "Brave New World", Category = StockCategory.Book, Price = 15.99m, Quantity = 10, ImageUrl = "/images/seeds/book-brave-new-world.jpg" },
                new StockItem { Name = "The Catcher in the Rye", Category = StockCategory.Book, Price = 16.99m, Quantity = 8, ImageUrl = "/images/seeds/book-catcher-rye.jpg" },
                new StockItem { Name = "The Great Gatsby", Category = StockCategory.Book, Price = 12.99m, Quantity = 20, ImageUrl = "/images/seeds/book-great-gatsby.jpg" },
                new StockItem { Name = "The Hobbit", Category = StockCategory.Book, Price = 18.99m, Quantity = 14, ImageUrl = "/images/seeds/book-hobbit.jpg" },
                new StockItem { Name = "The Lord of the Rings", Category = StockCategory.Book, Price = 24.99m, Quantity = 7, ImageUrl = "/images/seeds/book-lotr.jpg" },
                new StockItem { Name = "Moby Dick", Category = StockCategory.Book, Price = 17.99m, Quantity = 9, ImageUrl = "/images/seeds/book-moby-dick.jpg" },
                new StockItem { Name = "To Kill a Mockingbird", Category = StockCategory.Book, Price = 15.99m, Quantity = 11, ImageUrl = "/images/seeds/book-mockingbird.jpg" },
                new StockItem { Name = "Pride and Prejudice", Category = StockCategory.Book, Price = 14.99m, Quantity = 13, ImageUrl = "/images/seeds/book-pride-prejudice.jpg" },

                // Games
                new StockItem { Name = "Animal Crossing: New Horizons", Category = StockCategory.Game, Price = 49.99m, Quantity = 6, ImageUrl = "/images/seeds/game-animal-crossing.jpg" },
                new StockItem { Name = "Elden Ring", Category = StockCategory.Game, Price = 59.99m, Quantity = 8, ImageUrl = "/images/seeds/game-elden-ring.jpg" },
                new StockItem { Name = "Fortnite", Category = StockCategory.Game, Price = 10.23, Quantity = 17, ImageUrl = "/images/seeds/game-fortnite.jpeg" },
                new StockItem { Name = "God of War", Category = StockCategory.Game, Price = 39.99m, Quantity = 12, ImageUrl = "/images/seeds/game-god-of-war.jpg" },
                new StockItem { Name = "Halo Infinite", Category = StockCategory.Game, Price = 59.99m, Quantity = 10, ImageUrl = "/images/seeds/game-halo-infinite.png" },
                new StockItem { Name = "Super Mario Odyssey", Category = StockCategory.Game, Price = 49.99m, Quantity = 15, ImageUrl = "/images/seeds/game-mario-odyssey.jpg" },
                new StockItem { Name = "Minecraft", Category = StockCategory.Game, Price = 29.99m, Quantity = 25, ImageUrl = "/images/seeds/game-minecraft.jpg" },
                new StockItem { Name = "Stardew Valley", Category = StockCategory.Game, Price = 14.99m, Quantity = 20, ImageUrl = "/images/seeds/game-stardew.png" },
                new StockItem { Name = "The Witcher 3: Wild Hunt", Category = StockCategory.Game, Price = 39.99m, Quantity = 7, ImageUrl = "/images/seeds/game-witcher3.jpg" },
                new StockItem { Name = "The Legend of Zelda: Breath of the Wild", Category = StockCategory.Game, Price = 59.99m, Quantity = 9, ImageUrl = "/images/seeds/game-zelda.jpg" },

                // Toys
                new StockItem { Name = "Barbie Dreamhouse Doll", Category = StockCategory.Toy, Price = 24.99m, Quantity = 18, ImageUrl = "/images/seeds/toy-barbie.jpg" },
                new StockItem { Name = "Hot Wheels Car Set", Category = StockCategory.Toy, Price = 19.99m, Quantity = 22, ImageUrl = "/images/seeds/toy-hotwheels.jpg" },
                new StockItem { Name = "Jenga Classic Game", Category = StockCategory.Toy, Price = 12.99m, Quantity = 16, ImageUrl = "/images/seeds/toy-jenga.jpg" },
                new StockItem { Name = "LEGO Creator 3-in-1", Category = StockCategory.Toy, Price = 79.99m, Quantity = 8, ImageUrl = "/images/seeds/toy-lego.png" },
                new StockItem { Name = "Nerf Elite Blaster", Category = StockCategory.Toy, Price = 34.99m, Quantity = 12, ImageUrl = "/images/seeds/toy-nerf.jpg" },
                new StockItem { Name = "Play-Doh Modeling Compound", Category = StockCategory.Toy, Price = 9.99m, Quantity = 30, ImageUrl = "/images/seeds/toy-playdoh.webp" },
                new StockItem { Name = "RC Stunt Car", Category = StockCategory.Toy, Price = 45.99m, Quantity = 6, ImageUrl = "/images/seeds/toy-rc-car.jpeg" },
                new StockItem { Name = "Rubik's Cube 3x3", Category = StockCategory.Toy, Price = 14.99m, Quantity = 25, ImageUrl = "/images/seeds/toy-rubiks-cube.jpeg" },
                new StockItem { Name = "Classic Teddy Bear", Category = StockCategory.Toy, Price = 29.99m, Quantity = 14, ImageUrl = "/images/seeds/toy-teddy.jpg" },
                new StockItem { Name = "UNO Card Game", Category = StockCategory.Toy, Price = 7.99m, Quantity = 40, ImageUrl = "/images/seeds/toy-uno.jpg" }
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

            const string adminRole = "Admin";
            const string adminEmail = "admin@easygames.com";
            const string adminPassword = "Admin123!"; 

            if (!roleManager.RoleExistsAsync(adminRole).Result)
            {
                roleManager.CreateAsync(new IdentityRole(adminRole)).Wait();
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
        }
    }
}
