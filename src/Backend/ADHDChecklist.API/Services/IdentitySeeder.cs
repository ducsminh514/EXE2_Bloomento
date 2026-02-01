using ADHDChecklist.API.Entities.Common;
using Microsoft.AspNetCore.Identity;

namespace ADHDChecklist.API.Services
{
    public class IdentitySeeder
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public IdentitySeeder(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            // 1. Seed Roles
            await SeedRoleAsync("Admin");
            await SeedRoleAsync("User");

            // 2. Seed Admin User
            var adminEmail = "admin@bloomento.com";
            var adminUser = await _userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    IsEmailVerified = true, // Custom property required for Login
                    IsActive = true,
                    FullName = "Super Admin",
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(adminUser, "Admin@123"); // Change this in production!

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine("Seeded Admin User successfully.");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"Error seeding Admin User: {errors}");
                }
            }
            else
            {
                // Ensure existing admin has Admin role
                if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine("Added Admin role to existing Admin user.");
                }
            }
        }

        private async Task SeedRoleAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                Console.WriteLine($"Seeded Role: {roleName}");
            }
        }
    }
}
