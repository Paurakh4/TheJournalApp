using Reflecta.Models;

namespace Reflecta.Services.Interfaces;

public interface ISettingsService
{
    /// <summary>
    /// Get user settings
    /// </summary>
    Task<UserSettings?> GetSettingsAsync(Guid userId);

    /// <summary>
    /// Create default settings for new user
    /// </summary>
    Task<UserSettings> CreateDefaultSettingsAsync(Guid userId);

    /// <summary>
    /// Update theme preference
    /// </summary>
    Task<(bool Success, string Message)> UpdateThemeAsync(Guid userId, string theme);

    /// <summary>
    /// Update reminder settings
    /// </summary>
    Task<(bool Success, string Message)> UpdateReminderSettingsAsync(Guid userId, bool enabled, TimeOnly? reminderTime);

    /// <summary>
    /// Update user time zone
    /// </summary>
    Task<(bool Success, string Message)> UpdateTimeZoneAsync(Guid userId, string timeZoneId);

    /// <summary>
    /// Get user time zone (IANA or Windows ID). Defaults to UTC if not set.
    /// </summary>
    Task<string> GetTimeZoneAsync(Guid userId);

    /// <summary>
    /// Get current theme
    /// </summary>
    Task<string> GetThemeAsync(Guid userId);
}
