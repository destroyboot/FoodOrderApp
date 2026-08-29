using Core.Data.Entities;
using Infrastructure.Auth;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Seeding
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(
            AppDbContext db,
            UserManager<ApplicationUser> users,
            RoleManager<IdentityRole> roles)
        {
            var roleNames = new[] { "Admin", "RestaurantAdmin", "Waiter", "Chef", "DeliveryDriver", "Customer" };

            foreach (var roleName in roleNames)
            {
                if (!await roles.RoleExistsAsync(roleName))
                    await roles.CreateAsync(new IdentityRole(roleName));
            }

            var admin = await EnsureDevUserAsync(users, "admin@foodapp.local", "Admin123!", "admin");
            var chef = await EnsureDevUserAsync(users, "chef@foodapp.local", "Chef123!", "chef");
            var waiter = await EnsureDevUserAsync(users, "waiter@foodapp.local", "Waiter123!", "waiter");

            await EnsureIdentityRoleAsync(users, admin, "Admin");
            await EnsureIdentityRoleAsync(users, chef, "Chef");
            await EnsureIdentityRoleAsync(users, waiter, "Waiter");

            await EnsureRestaurantDriverAccountsAsync(db, users, "Pierogi Bistro", "pierogi");
            await EnsureRestaurantDriverAccountsAsync(db, users, "Sushi Garden", "sushi");
        }

        private static async Task<ApplicationUser> EnsureDevUserAsync(
            UserManager<ApplicationUser> users,
            string email,
            string password,
            string label)
        {
            var user = await users.FindByEmailAsync(email);
            if (user is not null)
                return user;

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var create = await users.CreateAsync(user, password);
            if (!create.Succeeded)
            {
                var errors = string.Join("; ", create.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create {label} user: {errors}");
            }

            return user;
        }

        private static async Task EnsureIdentityRoleAsync(
            UserManager<ApplicationUser> users,
            ApplicationUser user,
            string role)
        {
            if (!await users.IsInRoleAsync(user, role))
                await users.AddToRoleAsync(user, role);
        }

        private static async Task EnsureRestaurantDriverAccountsAsync(
            AppDbContext db,
            UserManager<ApplicationUser> users,
            string restaurantName,
            string emailPrefix)
        {
            var restaurant = await db.Restaurants
                .Where(x => x.IsActive && x.Name == restaurantName)
                .Select(x => new { x.Id, x.Name })
                .FirstOrDefaultAsync();

            if (restaurant is null)
                return;

            for (var number = 1; number <= 2; number++)
            {
                var driver = await EnsureDevUserAsync(
                    users,
                    $"{emailPrefix}.driver{number}@foodapp.local",
                    "Driver123!",
                    $"{restaurant.Name} delivery driver");

                await EnsureIdentityRoleAsync(users, driver, "DeliveryDriver");
                await EnsureRestaurantRoleAssignmentAsync(db, restaurant.Id, driver.Id, "DeliveryDriver");
            }
        }

        private static async Task EnsureRestaurantRoleAssignmentAsync(
            AppDbContext db,
            int restaurantId,
            string userId,
            string role)
        {
            var exists = await db.RestaurantUserRoles.AnyAsync(x =>
                x.RestaurantId == restaurantId &&
                x.UserId == userId &&
                x.Role == role);

            if (exists)
                return;

            db.RestaurantUserRoles.Add(new RestaurantUserRole
            {
                RestaurantId = restaurantId,
                UserId = userId,
                Role = role
            });

            await db.SaveChangesAsync();
        }
    }
}
