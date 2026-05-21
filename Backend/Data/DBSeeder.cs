using Microsoft.AspNetCore.Identity;
using SEAL.NET.Models.Entities;

namespace SEAL.NET.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            string[] roles =
            {
                "Admin",
                "Member",
                "TeamLeader",
                "Judge",
                "Mentor"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                }
            }

            var adminEmail = configuration["AdminBootstrap:Email"];
            var adminPassword = configuration["AdminBootstrap:Password"];
            var bootstrapAdminEnabled = configuration.GetValue<bool>("AdminBootstrap:Enabled");

            if (!bootstrapAdminEnabled)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException(
                    "Admin bootstrap is enabled, but AdminBootstrap:Email or AdminBootstrap:Password is not configured.");
            }

            if (adminPassword == "Admin@123456")
            {
                throw new InvalidOperationException("AdminBootstrap:Password must not use the default sample password.");
            }

            var admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Admin",
                    EmailConfirmed = true,
                    IsApproved = true
                };

                var result = await userManager.CreateAsync(admin, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Seed admin error: {error.Description}");
                    }
                }
            }
        }
    }
}
