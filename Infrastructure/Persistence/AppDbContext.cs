using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Data.Entities;
using Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
        public DbSet<MenuCategoryTranslation> MenuCategoryTranslations => Set<MenuCategoryTranslation>();

        public DbSet<MenuItem> MenuItems => Set<MenuItem>();
        public DbSet<MenuItemTranslation> MenuItemTranslations => Set<MenuItemTranslation>();

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Notification> Notifications => Set<Notification>();


        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // MenuCategory -> Translations (1 to many)
            b.Entity<MenuCategoryTranslation>()
                .HasIndex(x => new { x.MenuCategoryId, x.Culture })
                .IsUnique();

            b.Entity<MenuCategoryTranslation>()
                .HasOne<MenuCategory>()
                .WithMany(x => x.Translations)
                .HasForeignKey(x => x.MenuCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // MenuItem -> Translations (1 to many)
            b.Entity<MenuItemTranslation>()
                .HasIndex(x => new { x.MenuItemId, x.Culture })
                .IsUnique();

            b.Entity<MenuItemTranslation>()
                .HasOne<MenuItem>()
                .WithMany(x => x.Translations)
                .HasForeignKey(x => x.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // MenuItem -> MenuCategory (many to one)
            b.Entity<MenuItem>()
                .HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.MenuCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order -> OrderItem (1 to many)
            b.Entity<OrderItem>()
                .HasOne<Order>()
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Money/price fields
            b.Entity<MenuItem>()
                .Property(x => x.CurrentPrice)
                .HasPrecision(18, 2);

            b.Entity<Order>()
                .Property(x => x.Subtotal)
                .HasPrecision(18, 2);

            b.Entity<Order>()
                .Property(x => x.DeliveryFee)
                .HasPrecision(18, 2);

            b.Entity<Order>()
                .Property(x => x.Total)
                .HasPrecision(18, 2);

            b.Entity<OrderItem>()
                .Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            // Helpful indexes
            b.Entity<Order>()
                .HasIndex(o => new { o.Status, o.CreatedAt });

            b.Entity<MenuItem>()
                .HasIndex(m => new { m.MenuCategoryId, m.IsAvailable });

            //Notifcation
            b.Entity<Notification>(e =>
            {
                e.HasKey(x => x.Id);

                e.Property(x => x.OwnerKey).HasMaxLength(200).IsRequired();
                e.Property(x => x.Title).HasMaxLength(200).IsRequired();
                e.Property(x => x.Body).HasMaxLength(1000).IsRequired();

                e.HasIndex(x => new { x.OwnerKey, x.IsRead, x.CreatedAt });
            });

            b.Entity<ApplicationUser>(e =>
            {
                e.Property(x => x.RegistrationCodeHash).HasMaxLength(128);
            });

        }
    }
}
