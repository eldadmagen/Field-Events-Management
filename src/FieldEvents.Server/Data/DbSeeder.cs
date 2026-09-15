using FieldEvents.Server.Auth;
using FieldEvents.Server.Data.Entities;
using FieldEvents.Shared;
using Microsoft.EntityFrameworkCore;

namespace FieldEvents.Server.Data;

/// <summary>Demo users so the E2E flow and Swagger can be exercised immediately after a fresh clone. See README.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (await db.Users.AnyAsync())
        {
            return;
        }

        AddUser(db, "dispatcher1", "Passw0rd!", UserRole.Dispatcher);
        AddUser(db, "tech1", "Passw0rd!", UserRole.Technician);
        AddUser(db, "tech2", "Passw0rd!", UserRole.Technician);

        await db.SaveChangesAsync();
    }

    private static void AddUser(AppDbContext db, string userName, string password, UserRole role)
    {
        var (hash, salt) = PasswordHasher.Hash(password);
        db.Users.Add(new User { UserName = userName, PasswordHash = hash, PasswordSalt = salt, Role = role });
    }
}
