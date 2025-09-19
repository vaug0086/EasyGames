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
                new StockItem { Name = "The Pragmatic Programmer", Category = StockCategory.Book, Price = 79.95m, Quantity = 10, ImageUrl = "https://www.stockvault.net/data/2011/04/21/122334/preview16.jpg" },
                new StockItem { Name = "Catan", Category = StockCategory.Game, Price = 59.99m, Quantity = 8, ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQshBwRYwO46EpKtTh7e3Uj4xBQrdaJ8joKiw&s" },
                new StockItem { Name = "LEGO Classic Bricks", Category = StockCategory.Toy, Price = 39.00m, Quantity = 25, ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/1/19/Lego_bricks.jpg" }
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
