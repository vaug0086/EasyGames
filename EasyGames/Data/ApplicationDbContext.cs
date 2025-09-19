using EasyGames.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Data
{
    //  this class inherits from identitydbcontext<applicationuser identityrole string> so ef creates the full identity schema
    //  that includes tables for users roles userroles claims tokens etc out of the box
    //  by specifying applicationuser you extend identityuser with custom fields like fullname and dob
    //  added for stockitems orders and orderitems we can generate tables for entities alongside identity
    //  onmodelcreating calls base.onmodelcreating to ensure identity config is applied then adds project specific rules
    //  decimal precision is set for order.subtotal order.grandtotal orderitem.unitpriceatpurchase and stockitem.price
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<StockItem> StockItems => Set<StockItem>();

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<Order>(e =>
            {
                e.Property(o => o.Subtotal).HasPrecision(18, 2);
                e.Property(o => o.GrandTotal).HasPrecision(18, 2);
            });

            b.Entity<OrderItem>().Property(p => p.UnitPriceAtPurchase).HasPrecision(18, 2);
            b.Entity<StockItem>().Property(p => p.Price).HasPrecision(18, 2);
        }
    }
}