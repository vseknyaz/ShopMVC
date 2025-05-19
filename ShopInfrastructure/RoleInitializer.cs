using Microsoft.AspNetCore.Identity;
using ShopDomain.Model;
using ShopInfrastructure.Models;

namespace ShopInfrastructure
{
    public class RoleInitializer
    {
        public static async Task InitializeAsync(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            string adminEmail = "admin@shop.com";
            string password = "Admin123";

            // Ensure roles exist
            if (await roleManager.FindByNameAsync("Admin") == null)
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            if (await roleManager.FindByNameAsync("User") == null)
                await roleManager.CreateAsync(new IdentityRole("User"));

            var admin = await userManager.FindByEmailAsync(adminEmail) ?? await userManager.FindByNameAsync(adminEmail);
            if (admin == null)
            {
                admin = new User
                {
                    Email = adminEmail,
                    UserName = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, password);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }
            else
            {
                var currentRoles = await userManager.GetRolesAsync(admin);
                if (!currentRoles.Contains("Admin"))
                {
                    if (currentRoles.Any())
                        await userManager.RemoveFromRolesAsync(admin, currentRoles);
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
        }
    }
}