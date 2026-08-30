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
        public DbSet<MenuItemIngredient> MenuItemIngredients => Set<MenuItemIngredient>();
        public DbSet<Ingredient> Ingredients => Set<Ingredient>();
        public DbSet<IngredientTranslation> IngredientTranslations => Set<IngredientTranslation>();

        public DbSet<Restaurant> Restaurants => Set<Restaurant>();
        public DbSet<Cuisine> Cuisines => Set<Cuisine>();
        public DbSet<CuisineTranslation> CuisineTranslations => Set<CuisineTranslation>();
        public DbSet<Allergen> Allergens => Set<Allergen>();
        public DbSet<AllergenTranslation> AllergenTranslations => Set<AllergenTranslation>();
        public DbSet<AppLanguage> AppLanguages => Set<AppLanguage>();
        public DbSet<RestaurantSettings> RestaurantSettings => Set<RestaurantSettings>();
        public DbSet<RestaurantTable> RestaurantTables => Set<RestaurantTable>();
        public DbSet<RestaurantUserRole> RestaurantUserRoles => Set<RestaurantUserRole>();
        public DbSet<RestaurantStaffInvite> RestaurantStaffInvites => Set<RestaurantStaffInvite>();
        public DbSet<AdminTablePermission> AdminTablePermissions => Set<AdminTablePermission>();
        public DbSet<Reservation> Reservations => Set<Reservation>();
        public DbSet<ReservationSlot> ReservationSlots => Set<ReservationSlot>();
        public DbSet<ReservationSchedule> ReservationSchedules => Set<ReservationSchedule>();
        public DbSet<OrderTypeOption> OrderTypeOptions => Set<OrderTypeOption>();
        public DbSet<OrderTypeOptionTranslation> OrderTypeOptionTranslations => Set<OrderTypeOptionTranslation>();
        public DbSet<PaymentMethodOption> PaymentMethodOptions => Set<PaymentMethodOption>();
        public DbSet<PaymentMethodOptionTranslation> PaymentMethodOptionTranslations => Set<PaymentMethodOptionTranslation>();

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<OrderBillingDetails> OrderBillingDetails => Set<OrderBillingDetails>();
        public DbSet<OrderInvoiceDocument> OrderInvoiceDocuments => Set<OrderInvoiceDocument>();
        public DbSet<OrderComment> OrderComments => Set<OrderComment>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<PushDeviceRegistration> PushDeviceRegistrations => Set<PushDeviceRegistration>();


        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<Restaurant>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.Property(x => x.Address).HasMaxLength(500);
                e.Property(x => x.City).HasMaxLength(120);
                e.Property(x => x.Street).HasMaxLength(160);
                e.Property(x => x.PostalCode).HasMaxLength(40);
                e.Property(x => x.HouseNumber).HasMaxLength(40);
                e.Property(x => x.CuisineType).HasMaxLength(120);
            });

            b.Entity<Cuisine>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(120).IsRequired();
                e.HasIndex(x => x.Name).IsUnique();
            });

            b.Entity<CuisineTranslation>(e =>
            {
                e.Property(x => x.Culture).HasMaxLength(20).IsRequired();
                e.Property(x => x.Name).HasMaxLength(120).IsRequired();
                e.HasIndex(x => new { x.CuisineId, x.Culture }).IsUnique();
                e.HasOne<Cuisine>()
                    .WithMany(x => x.Translations)
                    .HasForeignKey(x => x.CuisineId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<Allergen>(e =>
            {
                e.Property(x => x.Code).HasMaxLength(50).IsRequired();
                e.Property(x => x.Name).HasMaxLength(120).IsRequired();
                e.HasIndex(x => x.Code).IsUnique();
            });

            b.Entity<AllergenTranslation>(e =>
            {
                e.Property(x => x.Culture).HasMaxLength(20).IsRequired();
                e.Property(x => x.Name).HasMaxLength(120).IsRequired();
                e.HasIndex(x => new { x.AllergenId, x.Culture }).IsUnique();
                e.HasOne<Allergen>()
                    .WithMany(x => x.Translations)
                    .HasForeignKey(x => x.AllergenId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<AppLanguage>(e =>
            {
                e.Property(x => x.Culture).HasMaxLength(20).IsRequired();
                e.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
                e.Property(x => x.NativeName).HasMaxLength(120).IsRequired();
                e.HasIndex(x => x.Culture).IsUnique();
            });

            b.Entity<RestaurantSettings>(e =>
            {
                e.Property(x => x.SupportedCultures).HasMaxLength(200).IsRequired();
                e.Property(x => x.DefaultCulture).HasMaxLength(20).IsRequired();
                e.Property(x => x.DeliveryFee).HasPrecision(18, 2);
                e.Property(x => x.DeliveryRadiusKm).HasPrecision(18, 2);
                e.Property(x => x.MinimumDeliveryOrder).HasPrecision(18, 2);
                e.Property(x => x.ExtraIngredientPrice).HasPrecision(18, 2);
                e.HasIndex(x => x.RestaurantId).IsUnique();
                e.HasOne(x => x.Restaurant)
                    .WithOne(x => x.Settings)
                    .HasForeignKey<RestaurantSettings>(x => x.RestaurantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<RestaurantTable>(e =>
            {
                e.Property(x => x.Label).HasMaxLength(80).IsRequired();
                e.HasIndex(x => new { x.RestaurantId, x.Label }).IsUnique();
                e.HasOne(x => x.Restaurant)
                    .WithMany(x => x.Tables)
                    .HasForeignKey(x => x.RestaurantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<RestaurantUserRole>(e =>
            {
                e.Property(x => x.UserId).HasMaxLength(450).IsRequired();
                e.Property(x => x.Role).HasMaxLength(80).IsRequired();
                e.HasIndex(x => new { x.RestaurantId, x.UserId, x.Role }).IsUnique();
                e.HasOne(x => x.Restaurant)
                    .WithMany(x => x.UserRoles)
                    .HasForeignKey(x => x.RestaurantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<RestaurantStaffInvite>(e =>
            {
                e.Property(x => x.Email).HasMaxLength(256).IsRequired();
                e.Property(x => x.RequestedRole).HasMaxLength(80).IsRequired();
                e.Property(x => x.InviteToken).HasMaxLength(120).IsRequired();
                e.Property(x => x.UserId).HasMaxLength(450);
                e.Property(x => x.InvitedByUserId).HasMaxLength(450).IsRequired();
                e.HasIndex(x => x.InviteToken).IsUnique();
                e.HasIndex(x => new { x.RestaurantId, x.Email, x.RequestedRole });
                e.HasOne(x => x.Restaurant)
                    .WithMany(x => x.StaffInvites)
                    .HasForeignKey(x => x.RestaurantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<AdminTablePermission>(e =>
            {
                e.Property(x => x.RoleName).HasMaxLength(80).IsRequired();
                e.Property(x => x.TableName).HasMaxLength(200).IsRequired();
                e.HasIndex(x => new { x.RoleName, x.TableName }).IsUnique();
            });

            b.Entity<OrderTypeOption>(e =>
            {
                e.Property(x => x.Code).HasMaxLength(80).IsRequired();
                e.HasIndex(x => x.Value).IsUnique();
                e.HasIndex(x => x.Code).IsUnique();
            });

            b.Entity<OrderTypeOptionTranslation>(e =>
            {
                e.Property(x => x.Culture).HasMaxLength(20).IsRequired();
                e.Property(x => x.Name).HasMaxLength(120).IsRequired();
                e.HasIndex(x => new { x.OrderTypeOptionId, x.Culture }).IsUnique();
                e.HasOne<OrderTypeOption>()
                    .WithMany(x => x.Translations)
                    .HasForeignKey(x => x.OrderTypeOptionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<PaymentMethodOption>(e =>
            {
                e.Property(x => x.Code).HasMaxLength(80).IsRequired();
                e.HasIndex(x => x.Value).IsUnique();
                e.HasIndex(x => x.Code).IsUnique();
            });

            b.Entity<PaymentMethodOptionTranslation>(e =>
            {
                e.Property(x => x.Culture).HasMaxLength(20).IsRequired();
                e.Property(x => x.Name).HasMaxLength(120).IsRequired();
                e.HasIndex(x => new { x.PaymentMethodOptionId, x.Culture }).IsUnique();
                e.HasOne<PaymentMethodOption>()
                    .WithMany(x => x.Translations)
                    .HasForeignKey(x => x.PaymentMethodOptionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<Reservation>(e =>
            {
                e.Property(x => x.GuestName).HasMaxLength(200).IsRequired();
                e.Property(x => x.GuestEmail).HasMaxLength(256);
                e.Property(x => x.GuestPhone).HasMaxLength(80);
                e.Property(x => x.Note).HasMaxLength(1000);
                e.HasIndex(x => new { x.RestaurantId, x.StartAt, x.Status });
                e.HasIndex(x => new { x.RestaurantTableId, x.StartAt, x.EndAt, x.Status });
                e.HasOne(x => x.Restaurant)
                    .WithMany()
                    .HasForeignKey(x => x.RestaurantId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.RestaurantTable)
                    .WithMany()
                    .HasForeignKey(x => x.RestaurantTableId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<ReservationSlot>(e =>
            {
                e.HasIndex(x => new { x.RestaurantTableId, x.StartAt }).IsUnique();
                e.HasIndex(x => new { x.RestaurantId, x.StartAt, x.IsActive });
                e.HasOne(x => x.Restaurant)
                    .WithMany()
                    .HasForeignKey(x => x.RestaurantId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.RestaurantTable)
                    .WithMany()
                    .HasForeignKey(x => x.RestaurantTableId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<ReservationSchedule>(e =>
            {
                e.HasIndex(x => new { x.RestaurantId, x.RestaurantTableId, x.StartMinuteOfDay, x.EndMinuteOfDay, x.IntervalMinutes })
                    .IsUnique();
                e.HasOne(x => x.Restaurant)
                    .WithMany()
                    .HasForeignKey(x => x.RestaurantId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.RestaurantTable)
                    .WithMany()
                    .HasForeignKey(x => x.RestaurantTableId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<MenuCategory>()
                .HasOne(x => x.Restaurant)
                .WithMany()
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<MenuCategoryTranslation>()
                .HasIndex(x => new { x.MenuCategoryId, x.Culture })
                .IsUnique();

            b.Entity<MenuCategoryTranslation>()
                .HasOne<MenuCategory>()
                .WithMany(x => x.Translations)
                .HasForeignKey(x => x.MenuCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<MenuItemTranslation>()
                .HasIndex(x => new { x.MenuItemId, x.Culture })
                .IsUnique();

            b.Entity<MenuItemTranslation>()
                .HasOne<MenuItem>()
                .WithMany(x => x.Translations)
                .HasForeignKey(x => x.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<Ingredient>(e =>
            {
                e.Property(x => x.CostPerUnit).HasPrecision(18, 4);
                e.Property(x => x.AllergenCode).HasMaxLength(100);
                e.HasIndex(x => new { x.RestaurantId, x.IsActive });
                e.HasOne(x => x.Restaurant)
                    .WithMany()
                    .HasForeignKey(x => x.RestaurantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<IngredientTranslation>(e =>
            {
                e.Property(x => x.Culture).HasMaxLength(20).IsRequired();
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.HasIndex(x => new { x.IngredientId, x.Culture }).IsUnique();
                e.HasOne<Ingredient>()
                    .WithMany(x => x.Translations)
                    .HasForeignKey(x => x.IngredientId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<MenuItemIngredient>(e =>
            {
                e.Property(x => x.Quantity).HasPrecision(18, 4);
                e.HasIndex(x => new { x.MenuItemId, x.IngredientId, x.IsDefault, x.IsSubstitute }).IsUnique();
                e.HasOne(x => x.MenuItem)
                    .WithMany(x => x.Ingredients)
                    .HasForeignKey(x => x.MenuItemId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Ingredient)
                    .WithMany()
                    .HasForeignKey(x => x.IngredientId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<MenuItem>()
                .HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.MenuCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<MenuItem>(e =>
            {
                e.Property(x => x.PhotoPath).HasMaxLength(300);
                e.HasIndex(x => new { x.MenuCategoryId, x.SortOrder, x.Id });
            });

            b.Entity<OrderItem>()
                .HasOne<Order>()
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<Order>()
                .HasOne(x => x.Restaurant)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.RestaurantId);

            b.Entity<Order>()
                .HasOne(x => x.Reservation)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<OrderBillingDetails>(e =>
            {
                e.Property(x => x.ReceiptEmail).HasMaxLength(256);
                e.Property(x => x.PersonName).HasMaxLength(200);
                e.Property(x => x.CompanyName).HasMaxLength(250);
                e.Property(x => x.TaxId).HasMaxLength(80);
                e.Property(x => x.BillingAddressLine1).HasMaxLength(250);
                e.Property(x => x.BillingAddressLine2).HasMaxLength(250);
                e.Property(x => x.BillingCity).HasMaxLength(120);
                e.Property(x => x.BillingPostalCode).HasMaxLength(40);
                e.Property(x => x.BillingCountry).HasMaxLength(80);
                e.HasIndex(x => x.OrderId).IsUnique();
                e.HasOne(x => x.Order)
                    .WithOne(x => x.BillingDetails)
                    .HasForeignKey<OrderBillingDetails>(x => x.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<OrderInvoiceDocument>(e =>
            {
                e.Property(x => x.InvoiceNumber).HasMaxLength(80).IsRequired();
                e.Property(x => x.FileName).HasMaxLength(200).IsRequired();
                e.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
                e.HasIndex(x => x.OrderId).IsUnique();
                e.HasIndex(x => x.InvoiceNumber).IsUnique();
                e.HasOne(x => x.Order)
                    .WithOne(x => x.InvoiceDocument)
                    .HasForeignKey<OrderInvoiceDocument>(x => x.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<OrderComment>(e =>
            {
                e.Property(x => x.AuthorUserId).HasMaxLength(450);
                e.Property(x => x.AuthorRole).HasMaxLength(80);
                e.Property(x => x.Body).HasMaxLength(2000).IsRequired();
                e.HasIndex(x => new { x.OrderId, x.CreatedAt });
                e.HasOne(x => x.Order)
                    .WithMany(x => x.Comments)
                    .HasForeignKey(x => x.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

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

            b.Entity<Order>()
                .Property(x => x.PickupContactName)
                .HasMaxLength(200);

            b.Entity<Order>()
                .Property(x => x.PickupPhone)
                .HasMaxLength(80);

            b.Entity<Order>()
                .Property(x => x.PickupNote)
                .HasMaxLength(1000);

            b.Entity<Order>()
                .Property(x => x.DeliveryContactName)
                .HasMaxLength(200);

            b.Entity<Order>()
                .Property(x => x.DeliveryPhone)
                .HasMaxLength(80);

            b.Entity<Order>()
                .Property(x => x.DeliveryAddressLine1)
                .HasMaxLength(250);

            b.Entity<Order>()
                .Property(x => x.DeliveryAddressLine2)
                .HasMaxLength(250);

            b.Entity<Order>()
                .Property(x => x.DeliveryCity)
                .HasMaxLength(120);

            b.Entity<Order>()
                .Property(x => x.DeliveryPostalCode)
                .HasMaxLength(40);

            b.Entity<Order>()
                .Property(x => x.DeliveryCountry)
                .HasMaxLength(80);

            b.Entity<Order>()
                .Property(x => x.DeliveryNote)
                .HasMaxLength(1000);

            b.Entity<Order>()
                .Property(x => x.AssignedDeliveryDriverUserId)
                .HasMaxLength(450);

            b.Entity<OrderItem>()
                .Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            b.Entity<OrderItem>()
                .Property(x => x.ExtraCharge)
                .HasPrecision(18, 2);

            b.Entity<OrderItem>()
                .Property(x => x.CustomizationsJson)
                .HasMaxLength(4000);

            b.Entity<Order>()
                .HasIndex(o => new { o.Status, o.CreatedAt });

            b.Entity<Order>()
                .HasIndex(o => new { o.RestaurantId, o.Status, o.CreatedAt });

            b.Entity<MenuItem>()
                .HasIndex(m => new { m.MenuCategoryId, m.IsAvailable });

            b.Entity<MenuCategory>()
                .HasIndex(c => new { c.RestaurantId, c.IsActive, c.SortOrder });

            b.Entity<Notification>(e =>
            {
                e.HasKey(x => x.Id);

                e.Property(x => x.OwnerKey).HasMaxLength(200).IsRequired();
                e.Property(x => x.Title).HasMaxLength(200).IsRequired();
                e.Property(x => x.Body).HasMaxLength(1000).IsRequired();

                e.HasIndex(x => new { x.OwnerKey, x.IsRead, x.CreatedAt });
            });

            b.Entity<PushDeviceRegistration>(e =>
            {
                e.Property(x => x.OwnerKey).HasMaxLength(200).IsRequired();
                e.Property(x => x.ExpoPushToken).HasMaxLength(300).IsRequired();
                e.Property(x => x.Platform).HasMaxLength(40).IsRequired();
                e.Property(x => x.DeviceName).HasMaxLength(200);
                e.Property(x => x.LastError).HasMaxLength(500);
                e.HasIndex(x => new { x.OwnerKey, x.ExpoPushToken }).IsUnique();
                e.HasIndex(x => new { x.OwnerKey, x.IsActive, x.LastSeenAt });
            });

            b.Entity<ApplicationUser>(e =>
            {
                e.Property(x => x.RegistrationCodeHash).HasMaxLength(128);
                e.Property(x => x.DefaultBillingReceiptEmail).HasMaxLength(256);
                e.Property(x => x.DefaultBillingPersonName).HasMaxLength(200);
                e.Property(x => x.DefaultBillingCompanyName).HasMaxLength(250);
                e.Property(x => x.DefaultBillingTaxId).HasMaxLength(80);
                e.Property(x => x.DefaultBillingAddressLine1).HasMaxLength(250);
                e.Property(x => x.DefaultBillingAddressLine2).HasMaxLength(250);
                e.Property(x => x.DefaultBillingCity).HasMaxLength(120);
                e.Property(x => x.DefaultBillingPostalCode).HasMaxLength(40);
                e.Property(x => x.DefaultBillingCountry).HasMaxLength(80);
                e.Property(x => x.DefaultDeliveryContactName).HasMaxLength(200);
                e.Property(x => x.DefaultDeliveryPhone).HasMaxLength(80);
                e.Property(x => x.DefaultDeliveryAddressLine1).HasMaxLength(250);
                e.Property(x => x.DefaultDeliveryAddressLine2).HasMaxLength(250);
                e.Property(x => x.DefaultDeliveryCity).HasMaxLength(120);
                e.Property(x => x.DefaultDeliveryPostalCode).HasMaxLength(40);
                e.Property(x => x.DefaultDeliveryCountry).HasMaxLength(80);
                e.Property(x => x.PreferredCulture).HasMaxLength(20);
                e.Property(x => x.AccountDeletionCodeHash).HasMaxLength(128);
            });

        }
    }
}
