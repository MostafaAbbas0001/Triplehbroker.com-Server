using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebsiteAPI.Data;
using WebsiteAPI.Entities;
using WebsiteAPI.Enums;

namespace WebsiteAPI.Services;

public class AdminSeeder(
    ApplicationDbContext context,
    IPasswordHasher<User> passwordHasher,
    IConfiguration configuration,
    ILogger<AdminSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var name = configuration["AdminSeed:Name"]?.Trim();
        var email = configuration["AdminSeed:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(name) &&
            string.IsNullOrWhiteSpace(email) &&
            string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Admin seeding is disabled. Configure AdminSeed__Name, AdminSeed__Email, and AdminSeed__Password in .env.");
            return;
        }

        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "AdminSeed__Name, AdminSeed__Email, and AdminSeed__Password must all be configured in .env.");
        }

        if (password.Length < 12)
        {
            throw new InvalidOperationException(
                "AdminSeed__Password must contain at least 12 characters.");
        }

        var existingUser = await context.Users.SingleOrDefaultAsync(
            user => EF.Functions.ILike(user.Email, email),
            cancellationToken);

        if (existingUser is not null)
        {
            if (existingUser.Role != UserRole.Admin)
            {
                throw new InvalidOperationException(
                    $"The configured admin email '{email}' belongs to a non-admin user.");
            }

            logger.LogInformation("Seed admin '{Email}' already exists.", email);
            return;
        }

        var admin = new User
        {
            Name = name,
            Email = email,
            Role = UserRole.Admin
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, password);

        context.Users.Add(admin);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seed admin '{Email}' was created.", email);
    }
}
