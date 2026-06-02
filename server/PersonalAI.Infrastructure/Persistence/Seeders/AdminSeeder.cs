using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonalAI.Domain.Enums;
using PersonalAI.Infrastructure.Identity;
using PersonalAI.Infrastructure.Persistence.Contexts;

namespace PersonalAI.Infrastructure.Persistence.Seeders;

public static class AdminSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        var passwordHasher = serviceProvider.GetRequiredService<IPasswordHasher<ApplicationUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        const string adminEmail = "admin@personalai.com";
        var existingAdmin = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (existingAdmin != null) return;

        var adminPassword = configuration["Seed:AdminPassword"] ?? "Admin@123456";

        var adminUser = new ApplicationUser
        {
            Email = adminEmail,
            FirstName = "System",
            LastName = "Administrator",
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };

        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, adminPassword);

        dbContext.Users.Add(adminUser);
        await dbContext.SaveChangesAsync();
    }
}
