using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheJournalApp.Data;
using TheJournalApp.Services;

namespace TheJournalApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        builder.Services.AddMauiBlazorWebView();

        // Configure PostgreSQL database
        var connectionString = "Host=localhost;Database=thejournalapp;Username=paurakh;Include Error Detail=true";
        builder.Services.AddDbContext<JournalDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Register services
        builder.Services.AddScoped<JournalService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}