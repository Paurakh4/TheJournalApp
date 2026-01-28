using Microsoft.EntityFrameworkCore;
using Reflecta.Data;
using Reflecta.Models;
using Reflecta.Services.Interfaces;

namespace Reflecta.Services;

public class SettingsService : ISettingsService
{
    private readonly IDbContextFactory<ReflectaDbContext> _contextFactory;

    public SettingsService(IDbContextFactory<ReflectaDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<UserSettings?> GetSettingsAsync(Guid userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<UserSettings> CreateDefaultSettingsAsync(Guid userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existingSettings = await context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (existingSettings != null)
            return existingSettings;

        var settings = new UserSettings
        {
            UserId = userId,
            Theme = "dark",
            PinEnabled = false,
            ReminderEnabled = false,
            TimeZoneId = TimeZoneInfo.Local.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.UserSettings.Add(settings);
        await context.SaveChangesAsync();

        return settings;
    }

    public async Task<(bool Success, string Message)> UpdateThemeAsync(Guid userId, string theme)
    {
        if (theme != "light" && theme != "dark")
            return (false, "Invalid theme. Use 'light' or 'dark'");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var settings = await context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
        
        if (settings == null)
        {
            settings = new UserSettings
            {
                UserId = userId,
                Theme = "dark",
                PinEnabled = false,
                ReminderEnabled = false,
                TimeZoneId = TimeZoneInfo.Local.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.UserSettings.Add(settings);
        }

        settings.Theme = theme;
        settings.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return (true, "Theme updated successfully");
    }

    public async Task<(bool Success, string Message)> UpdateReminderSettingsAsync(
        Guid userId, bool enabled, TimeOnly? reminderTime)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var settings = await context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
        
        if (settings == null)
        {
            settings = new UserSettings
            {
                UserId = userId,
                Theme = "dark",
                PinEnabled = false,
                ReminderEnabled = false,
                TimeZoneId = TimeZoneInfo.Local.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.UserSettings.Add(settings);
        }

        settings.ReminderEnabled = enabled;
        settings.ReminderTime = enabled ? reminderTime : null;
        settings.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return (true, "Reminder settings updated successfully");
    }

    public async Task<(bool Success, string Message)> UpdateTimeZoneAsync(Guid userId, string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return (false, "Time zone is required");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var settings = await context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            settings = new UserSettings
            {
                UserId = userId,
                Theme = "dark",
                PinEnabled = false,
                ReminderEnabled = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.UserSettings.Add(settings);
        }

        settings.TimeZoneId = timeZoneId.Trim();
        settings.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return (true, "Time zone updated successfully");
    }

    public async Task<string> GetTimeZoneAsync(Guid userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var settings = await context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
        return string.IsNullOrWhiteSpace(settings?.TimeZoneId) ? "UTC" : settings!.TimeZoneId;
    }

    public async Task<string> GetThemeAsync(Guid userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var settings = await context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
        return settings?.Theme ?? "dark";
    }
}
