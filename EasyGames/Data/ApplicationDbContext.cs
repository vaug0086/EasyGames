using EasyGames.Models;
using EasyGames.Models.Emailing;
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
        public DbSet<Shop> Shops => Set<Shop>();
        public DbSet<ShopStock> ShopStock => Set<ShopStock>();

        //   // TIERING
        public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();

        public DbSet<EasyGames.Models.Emailing.CustomerGroup> CustomerGroups => Set<EasyGames.Models.Emailing.CustomerGroup>();
        public DbSet<EasyGames.Models.Emailing.CustomerGroupMember> CustomerGroupMembers => Set<EasyGames.Models.Emailing.CustomerGroupMember>();
        public DbSet<EasyGames.Models.Emailing.EmailCampaign> EmailCampaigns => Set<EasyGames.Models.Emailing.EmailCampaign>();
        public DbSet<EasyGames.Models.Emailing.EmailCampaignRecipient> EmailCampaignRecipients => Set<EasyGames.Models.Emailing.EmailCampaignRecipient>();

       
        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<Order>(e =>
            {
                e.Property(o => o.Subtotal).HasPrecision(18, 2);
                e.Property(o => o.GrandTotal).HasPrecision(18, 2);

                // ADDED: profit/cost totals need precision too
                e.Property(o => o.TotalCost).HasPrecision(18, 2);
                e.Property(o => o.TotalProfit).HasPrecision(18, 2);
            });

            // ApplicationDbContext.OnModelCreating
            b.Entity<EmailCampaign>().HasIndex(c => c.IsPublic);
            b.Entity<EmailCampaign>().HasIndex(c => c.PublishedUtc);


            b.Entity<OrderItem>().Property(p => p.UnitPriceAtPurchase).HasPrecision(18, 2);
            // ADDED: buy-cost snapshot on the line item
            b.Entity<OrderItem>().Property(p => p.UnitBuyPriceAtPurchase).HasPrecision(18, 2);

            b.Entity<StockItem>(e =>
            {
                e.Property(p => p.BuyPrice).HasPrecision(18, 2);
                e.Property(p => p.SellPrice).HasPrecision(18, 2);
            });

            // ADDED: lifetime contribution precision for tiering math
            b.Entity<CustomerProfile>()
             .Property(p => p.LifetimeProfitContribution)
             .HasPrecision(18, 2);

            // we need to explicitly configure Shop and ShopStock
            // while the other entities can have most of their schema details inferred, these have a lot more custom logic
            // we need to make sure data integrity is protected!
            // NOTE FROM FRANK - this code was generated with the support of Claude Code! No reference or prompt because we worked through it together (i.e., its not copy and paste)
            b.Entity<Shop>(e =>
            {
                e.HasOne(s => s.Proprietor)
                 .WithMany()
                 .HasForeignKey(s => s.ProprietorUserId) // string foreign key to Identity user
                 .OnDelete(DeleteBehavior.Restrict); // prevent deleting users who own shops
            });

            b.Entity<ShopStock>(e =>
            {
                e.Property(ss => ss.InheritedBuyPrice).HasPrecision(18, 2); // financial precision for money calculations
                e.Property(ss => ss.InheritedSellPrice).HasPrecision(18, 2); // financial precision for money calculations

                e.HasOne(ss => ss.Shop)
                 .WithMany(s => s.ShopStock)
                 .HasForeignKey(ss => ss.ShopId)
                 .OnDelete(DeleteBehavior.Cascade); // delete shop stock when shop is deleted

                e.HasOne(ss => ss.StockItem)
                 .WithMany()
                 .HasForeignKey(ss => ss.StockItemId)
                 .OnDelete(DeleteBehavior.Restrict); // prevent deleting stock items that are in shops

                e.HasIndex(ss => new { ss.ShopId, ss.StockItemId }).IsUnique(); // prevent duplicate stock items per shop

                e.ToTable(tb => tb.HasCheckConstraint(
                    "CK_ShopStock_QtyOnHand_NonNegative", "[QtyOnHand] >= 0"));// Prevents negative values for QtyonHand
            });

            // Order → OrderItems
            b.Entity<Order>()
             .HasMany(o => o.Items)
             .WithOne(oi => oi.Order)
             .HasForeignKey(oi => oi.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
