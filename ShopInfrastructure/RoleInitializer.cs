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
            if (await roleManager.FindByNameAsync("admin") == null)
                await roleManager.CreateAsync(new IdentityRole("admin"));
            if (await roleManager.FindByNameAsync("user") == null)
                await roleManager.CreateAsync(new IdentityRole("user"));
            if (await userManager.FindByNameAsync(adminEmail) == null)
            {
                var admin = new User
                {
                    Email = adminEmail,
                    UserName = adminEmail,
                    FirstName = "Admin",
                    LastName = "User"
                };
                var result = await userManager.CreateAsync(admin, password);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "admin");
            }
        }
    }
}