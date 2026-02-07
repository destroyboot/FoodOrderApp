using Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Seeding
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(
            UserManager<ApplicationUser> users,
            RoleManager<IdentityRole> roles)
        {
            // 1) Roles
            var roleNames = new[] { "Admin", "Waiter", "Chef", "Customer" };

            foreach (var roleName in roleNames)
            {
                if (!await roles.RoleExistsAsync(roleName))
                    await roles.CreateAsync(new IdentityRole(roleName));
            }

            // 2) Admin user (dev)
            const string adminEmail = "admin@foodapp.local";
            const string adminPassword = "Admin123!"; // DEV ONLY - change later

            const string chefEmail = "chef@foodapp.local";
            const string chefPassword = "Chef123!"; // DEV ONLY - change later

            const string waiterEmail = "waiter@foodapp.local";
            const string waiterPassword = "Waiter123!"; // DEV ONLY - change later

            var admin = await users.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var create = await users.CreateAsync(admin, adminPassword);
                if (!create.Succeeded)
                {
                    var errors = string.Join("; ", create.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create admin user: {errors}");
                }
            }

            // Ensure role assignment
            if (!await users.IsInRoleAsync(admin, "Admin"))
                await users.AddToRoleAsync(admin, "Admin");

            var chef = await users.FindByEmailAsync(chefEmail);
            if (chef is null)
            {
                chef = new ApplicationUser
                {
                    UserName = chefEmail,
                    Email = chefEmail,
                    EmailConfirmed = true
                };

                var create = await users.CreateAsync(chef, chefPassword);
                if (!create.Succeeded)
                {
                    var errors = string.Join("; ", create.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create chef user: {errors}");
                }
            }

            // Ensure role assignment
            if (!await users.IsInRoleAsync(chef, "Chef"))
                await users.AddToRoleAsync(chef, "Chef");

            var waiter = await users.FindByEmailAsync(waiterEmail);
            if (waiter is null)
            {
                waiter = new ApplicationUser
                {
                    UserName = waiterEmail,
                    Email = waiterEmail,
                    EmailConfirmed = true
                };

                var create = await users.CreateAsync(waiter, waiterPassword);
                if (!create.Succeeded)
                {
                    var errors = string.Join("; ", create.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create waiter user: {errors}");
                }
            }

            // Ensure role assignment
            if (!await users.IsInRoleAsync(waiter, "Waiter"))
                await users.AddToRoleAsync(waiter, "Waiter");
        }
    }
}
