using KhataFlow.Core.Domain.IdentityEntities;
using KhataFlow.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace KhataFlow.Infrastructure.Data;

public static class AdminUserSeeder
{
    public static async Task SeedAsync(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration config)
    {
        const string roleName = "Admin";

        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = roleName,
                Description = "Platform administrator"
            });
        }

        var adminEmail = config["AdminSeed:Email"] ?? "admin@khataflow.com";
        var existing = await userManager.FindByEmailAsync(adminEmail);
        if (existing is not null)
            return; 

        var adminPassword = config["AdminSeed:Password"]
            ?? throw new InvalidOperationException(
                "AdminSeed:Password is not configured. Set it via appsettings or an environment variable before first run.");

        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = "System Admin",
            Role = UserRole.SuperAdmin,
            Status = AccountStatus.Active,
            LockoutEnabled = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                "Failed to seed admin user: " + string.Join("; ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(adminUser, roleName);
    }
}