using Reflecta.Models;

namespace Reflecta.Services.Interfaces;

public interface IStreakService
{
    /// <summary>
    /// Get streak data for user
    /// </summary>
    Task<Streak?> GetStreakAsync(Guid userId);

    /// <summary>
    /// Update streak after creating/deleting an entry
    /// </summary>
    Task<Streak> UpdateStreakAsync(Guid userId, string? timeZoneId = null);

    /// <summary>
    /// Calculate and update streak based on all entries
    /// </summary>
    Task<Streak> RecalculateStreakAsync(Guid userId, string? timeZoneId = null);

    /// <summary>
    /// Get days active (total days with entries)
    /// </summary>
    Task<int> GetDaysActiveAsync(Guid userId);
    
    /// <summary>
    /// Get missed days that broke streaks within a date range.
    /// Only returns days that caused a streak to break (gap after an active day).
    /// </summary>
    Task<List<DateOnly>> GetStreakBreakingDaysAsync(Guid userId, DateOnly startDate, DateOnly endDate);
}
