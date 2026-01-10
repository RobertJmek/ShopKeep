using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ShopKeep.Models
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var db = services.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            await EnsureRoleAsync(roleManager, "Admin");
            await EnsureRoleAsync(roleManager, "Editor");
            await EnsureRoleAsync(roleManager, "User");

            await EnsureUserAsync(
                userManager,
                email: "admin@shopkeep.com",
                fullName: "Admin Robert",
                dateOfBirth: new DateTime(2003, 6, 9),
                password: "Admin1!",
                role: "Admin");

            await EnsureUserAsync(
                userManager,
                email: "editor@shopkeep.com",
                fullName: "Editor Elena",
                dateOfBirth: new DateTime(2003, 6, 9),
                password: "Editor1!",
                role: "Editor");

            await EnsureUserAsync(
                userManager,
                email: "user@gmail.com",
                fullName: "User Andrei",
                dateOfBirth: new DateTime(2003, 6, 9),
                password: "User1!",
                role: "User");
        }

        private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                return;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed creating role '{roleName}': {string.Join(", ", result.Errors)}");
            }
        }

        private static async Task EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string fullName,
            DateTime dateOfBirth,
            string password,
            string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = fullName,
                    DateOfBirth = dateOfBirth,
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed creating user '{email}': {string.Join(", ", createResult.Errors)}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, role);
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed adding user '{email}' to role '{role}': {string.Join(", ", roleResult.Errors)}");
                }
            }
        }
    }
}