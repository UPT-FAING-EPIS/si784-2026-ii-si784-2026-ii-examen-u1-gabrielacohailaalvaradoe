using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniversitySocial.Api.Models;

namespace UniversitySocial.Api.Data;

public static class DevelopmentSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration configuration)
    {
        if (await db.Users.AnyAsync()) return;
        var password = configuration["Demo:AdminPassword"];
        if (string.IsNullOrWhiteSpace(password)) return;
        var admin = new User
        {
            Email = configuration["Demo:AdminEmail"] ?? "admin@university.local",
            DisplayName = "Administración Demo",
            Role = Roles.Administrator,
            Bio = "Cuenta creada únicamente cuando se configura Demo:AdminPassword."
        };
        admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, password);
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}
