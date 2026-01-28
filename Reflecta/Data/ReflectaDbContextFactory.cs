using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Reflecta.Data;

/// <summary>
/// Design-time factory for EF Core CLI tools (migrations, database updates)
/// This is needed because MAUI projects don't use the standard ASP.NET Core startup
/// </summary>
public class ReflectaDbContextFactory : IDesignTimeDbContextFactory<ReflectaDbContext>
{
    public ReflectaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ReflectaDbContext>();
        
        // Use environment variable if provided, otherwise fall back to local username
        var connectionString = Environment.GetEnvironmentVariable("REFLECTA_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = $"Host=localhost;Port=5432;Database=reflecta_db;Username={Environment.UserName}";
        }

        optionsBuilder.UseNpgsql(connectionString);

        return new ReflectaDbContext(optionsBuilder.Options);
    }
}
