using System.Reflection;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using Reflecta.Auth;
using Reflecta.Data;
using Reflecta.Services;
using Reflecta.Services.Interfaces;

namespace Reflecta;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        // Load configuration from embedded appsettings.json
        var assembly = Assembly.GetExecutingAssembly();
        Stream? stream = assembly.GetManifestResourceStream("appsettings.json");
        if (stream is null)
        {
            stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").GetAwaiter().GetResult();
        }

        var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();
        
        builder.Configuration.AddConfiguration(config);

        // Configure PostgreSQL Database
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("REFLECTA_CONNECTION_STRING");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = $"Host=localhost;Port=5432;Database=reflecta_db;Username={Environment.UserName}";
        }
        else if (connectionString.Contains("YOUR_USERNAME", StringComparison.OrdinalIgnoreCase))
        {
            connectionString = connectionString.Replace("YOUR_USERNAME", Environment.UserName, StringComparison.OrdinalIgnoreCase);
        }

        builder.Services.AddDbContextFactory<ReflectaDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Register Services (order matters for dependencies)
        builder.Services.AddScoped<ISettingsService, SettingsService>();
        builder.Services.AddScoped<IStreakService, StreakService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IMoodService, MoodService>();
        builder.Services.AddScoped<ITagService, TagService>();
        builder.Services.AddScoped<IJournalService, JournalService>();
        builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
        builder.Services.AddScoped<IExportService, ExportService>();
        
        // Lock service needs to be singleton to maintain state across the app lifecycle
        builder.Services.AddSingleton<ILockService, LockService>();

        // Configure Authentication
        builder.Services.AddScoped<ReflectaAuthenticationStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(provider => 
            provider.GetRequiredService<ReflectaAuthenticationStateProvider>());
        builder.Services.AddAuthorizationCore();

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Auto-migrate database on startup
        using (var scope = app.Services.CreateScope())
        {
            var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ReflectaDbContext>>();
            try
            {
                using var dbContext = dbContextFactory.CreateDbContext();
                dbContext.Database.Migrate();
                EnsurePinLockColumns(dbContext);
                EnsureTagCategoryColumn(dbContext);
                EnsureTimeZoneColumn(dbContext);
                
                // Seed demo data for demo user (in separate try-catch to not block app startup)
                // Use Task.Run to avoid synchronization context deadlock on main thread
                try
                {
                    Task.Run(async () => await DemoDataSeeder.SeedDemoEntriesAsync(dbContext)).GetAwaiter().GetResult();
                }
                catch
                {
                    // Non-fatal: demo data seeding failed
                }
            }
            catch
            {
                // Non-fatal: database migration error
            }
        }

        return app;
    }

    private static void EnsurePinLockColumns(ReflectaDbContext dbContext)
    {
        try
        {
            dbContext.Database.ExecuteSqlRaw(
                "ALTER TABLE user_settings ADD COLUMN IF NOT EXISTS biometric_enabled boolean NOT NULL DEFAULT FALSE;");

            dbContext.Database.ExecuteSqlRaw(
                "ALTER TABLE user_settings ADD COLUMN IF NOT EXISTS lock_timeout_minutes integer NOT NULL DEFAULT 0;");
        }
        catch
        {
            // Column may already exist
        }
    }

    private static void EnsureTagCategoryColumn(ReflectaDbContext dbContext)
    {
        try
        {
            dbContext.Database.ExecuteSqlRaw(
                "ALTER TABLE tags ADD COLUMN IF NOT EXISTS category character varying(50);");

            dbContext.Database.ExecuteSqlRaw(@"
                UPDATE tags SET category = 'Work' WHERE is_system = true AND (category IS NULL OR category = '') AND LOWER(name) = 'work';
                UPDATE tags SET category = 'Personal' WHERE is_system = true AND (category IS NULL OR category = '') AND LOWER(name) IN ('personal', 'goals');
                UPDATE tags SET category = 'Health' WHERE is_system = true AND (category IS NULL OR category = '') AND LOWER(name) IN ('health', 'mindfulness');
                UPDATE tags SET category = 'Relationships' WHERE is_system = true AND (category IS NULL OR category = '') AND LOWER(name) IN ('family', 'relationships');
                UPDATE tags SET category = 'Travel' WHERE is_system = true AND (category IS NULL OR category = '') AND LOWER(name) = 'travel';
                UPDATE tags SET category = 'Lifestyle' WHERE is_system = true AND (category IS NULL OR category = '') AND LOWER(name) IN ('learning', 'creativity');
                UPDATE tags SET category = 'Personal' WHERE is_system = true AND (category IS NULL OR category = '');
            ");
        }
        catch
        {
            // Column may already exist
        }
    }

    private static void EnsureTimeZoneColumn(ReflectaDbContext dbContext)
    {
        try
        {
            dbContext.Database.ExecuteSqlRaw(
                "ALTER TABLE user_settings ADD COLUMN IF NOT EXISTS time_zone_id character varying(100);");
        }
        catch
        {
            // Column may already exist
        }
    }
}
