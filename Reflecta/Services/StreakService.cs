using Microsoft.EntityFrameworkCore;
using Reflecta.Data;
using Reflecta.Models;
using Reflecta.Services.Interfaces;

namespace Reflecta.Services;

public class StreakService : IStreakService
{
    private readonly IDbContextFactory<ReflectaDbContext> _contextFactory;

    public StreakService(IDbContextFactory<ReflectaDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Streak?> GetStreakAsync(Guid userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var streak = await context.Streaks.FirstOrDefaultAsync(s => s.UserId == userId);
        
        if (streak != null)
        {
            // Check if streak needs to be reset (missed a day)
            var timeZoneId = await GetUserTimeZoneIdAsync(context, userId);
            var today = DateOnly.FromDateTime(GetUserLocalNow(timeZoneId));
            if (streak.LastEntryDate.HasValue)
            {
                var daysSinceLastEntry = today.DayNumber - streak.LastEntryDate.Value.DayNumber;
                
                // If more than 1 day has passed, reset current streak
                if (daysSinceLastEntry > 1)
                {
                    streak.CurrentStreak = 0;
                    streak.StreakStartDate = null;
                    streak.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
            }
        }

        return streak;
    }

    public async Task<Streak> UpdateStreakAsync(Guid userId, string? timeZoneId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var streak = await context.Streaks.FirstOrDefaultAsync(s => s.UserId == userId);
        var today = DateOnly.FromDateTime(GetUserLocalNow(timeZoneId));

        if (streak == null)
        {
            streak = new Streak
            {
                UserId = userId,
                CurrentStreak = 1,
                LongestStreak = 1,
                LastEntryDate = today,
                StreakStartDate = today,
                UpdatedAt = DateTime.UtcNow
            };
            context.Streaks.Add(streak);
        }
        else
        {
            if (streak.LastEntryDate == today)
            {
                // Entry already exists for today, no streak update needed
                return streak;
            }

            if (streak.LastEntryDate.HasValue)
            {
                var daysSinceLastEntry = today.DayNumber - streak.LastEntryDate.Value.DayNumber;

                if (daysSinceLastEntry == 1)
                {
                    // Consecutive day - increment streak
                    streak.CurrentStreak++;
                }
                else if (daysSinceLastEntry > 1)
                {
                    // Missed day(s) - reset streak
                    streak.CurrentStreak = 1;
                    streak.StreakStartDate = today;
                }
                // If daysSinceLastEntry == 0, entry was already made today (handled above)
            }
            else
            {
                // First entry ever
                streak.CurrentStreak = 1;
                streak.StreakStartDate = today;
            }

            // Update longest streak if needed
            if (streak.CurrentStreak > streak.LongestStreak)
            {
                streak.LongestStreak = streak.CurrentStreak;
            }

            streak.LastEntryDate = today;
            streak.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        return streak;
    }

    public async Task<Streak> RecalculateStreakAsync(Guid userId, string? timeZoneId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var streak = await context.Streaks.FirstOrDefaultAsync(s => s.UserId == userId);
        
        if (streak == null)
        {
            streak = new Streak
            {
                UserId = userId,
                CurrentStreak = 0,
                LongestStreak = 0,
                UpdatedAt = DateTime.UtcNow
            };
            context.Streaks.Add(streak);
            await context.SaveChangesAsync();
            return streak;
        }

        // Get all entry dates ordered
        var entryDates = await context.JournalEntries
            .Where(e => e.UserId == userId)
            .Select(e => e.EntryDate)
            .OrderBy(d => d)
            .ToListAsync();

        if (!entryDates.Any())
        {
            streak.CurrentStreak = 0;
            streak.LongestStreak = 0;
            streak.LastEntryDate = null;
            streak.StreakStartDate = null;
            streak.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return streak;
        }

        // Calculate longest streak
        int currentStreak = 1;
        int longestStreak = 1;
        DateOnly streakStart = entryDates[0];
        DateOnly longestStreakStart = entryDates[0];
        DateOnly longestStreakEnd = entryDates[0];

        for (int i = 1; i < entryDates.Count; i++)
        {
            var dayDiff = entryDates[i].DayNumber - entryDates[i - 1].DayNumber;
            
            if (dayDiff == 1)
            {
                currentStreak++;
            }
            else
            {
                if (currentStreak > longestStreak)
                {
                    longestStreak = currentStreak;
                    longestStreakStart = streakStart;
                    longestStreakEnd = entryDates[i - 1];
                }
                currentStreak = 1;
                streakStart = entryDates[i];
            }
        }

        // Check final streak
        if (currentStreak > longestStreak)
        {
            longestStreak = currentStreak;
        }

        // Calculate current streak (from today going backwards)
        var today = DateOnly.FromDateTime(GetUserLocalNow(timeZoneId));
        var lastEntryDate = entryDates.Last();
        
        int calculatedCurrentStreak = 0;
        DateOnly? currentStreakStart = null;

        if (lastEntryDate == today || (today.DayNumber - lastEntryDate.DayNumber) == 1)
        {
            // There's an entry today or yesterday, calculate current streak
            calculatedCurrentStreak = 1;
            currentStreakStart = lastEntryDate;

            for (int i = entryDates.Count - 2; i >= 0; i--)
            {
                var dayDiff = entryDates[i + 1].DayNumber - entryDates[i].DayNumber;
                if (dayDiff == 1)
                {
                    calculatedCurrentStreak++;
                    currentStreakStart = entryDates[i];
                }
                else
                {
                    break;
                }
            }
        }

        streak.CurrentStreak = calculatedCurrentStreak;
        streak.LongestStreak = longestStreak;
        streak.LastEntryDate = lastEntryDate;
        streak.StreakStartDate = currentStreakStart;
        streak.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return streak;
    }

    public async Task<int> GetDaysActiveAsync(Guid userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.JournalEntries
            .Where(e => e.UserId == userId)
            .Select(e => e.EntryDate)
            .Distinct()
            .CountAsync();
    }

    public async Task<List<DateOnly>> GetStreakBreakingDaysAsync(Guid userId, DateOnly startDate, DateOnly endDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        // Get all entry dates for the user, ordered
        var allEntryDates = await context.JournalEntries
            .Where(e => e.UserId == userId)
            .Select(e => e.EntryDate)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        if (allEntryDates.Count < 2)
        {
            // No streaks possible with 0 or 1 entries
            return new List<DateOnly>();
        }

        var streakBreakingDays = new List<DateOnly>();
        var timeZoneId = await GetUserTimeZoneIdAsync(context, userId);
        var today = DateOnly.FromDateTime(GetUserLocalNow(timeZoneId));

        // Find the first day after a gap of more than 1 day (this is where a streak broke)
        // A streak-breaking day is the day AFTER an entry where the user missed journaling
        for (int i = 1; i < allEntryDates.Count; i++)
        {
            var previousEntry = allEntryDates[i - 1];
            var currentEntry = allEntryDates[i];
            var dayGap = currentEntry.DayNumber - previousEntry.DayNumber;

            if (dayGap > 1)
            {
                // There was a gap - the day after previousEntry is when the streak broke
                var streakBrokeOn = previousEntry.AddDays(1);
                
                // Only include if it falls within the requested range
                if (streakBrokeOn >= startDate && streakBrokeOn <= endDate)
                {
                    streakBreakingDays.Add(streakBrokeOn);
                }
            }
        }

        // Also check if the current streak is broken (last entry was more than 1 day ago)
        if (allEntryDates.Count > 0)
        {
            var lastEntry = allEntryDates.Last();
            var daysSinceLastEntry = today.DayNumber - lastEntry.DayNumber;
            
            if (daysSinceLastEntry > 1)
            {
                // The streak broke the day after the last entry
                var streakBrokeOn = lastEntry.AddDays(1);
                
                if (streakBrokeOn >= startDate && streakBrokeOn <= endDate && !streakBreakingDays.Contains(streakBrokeOn))
                {
                    streakBreakingDays.Add(streakBrokeOn);
                }
            }
        }

        return streakBreakingDays.OrderBy(d => d).ToList();
    }

    private static async Task<string> GetUserTimeZoneIdAsync(ReflectaDbContext context, Guid userId)
    {
        var timeZoneId = await context.UserSettings
            .Where(s => s.UserId == userId)
            .Select(s => s.TimeZoneId)
            .FirstOrDefaultAsync();

        return string.IsNullOrWhiteSpace(timeZoneId) ? "UTC" : timeZoneId;
    }

    private static DateTime GetUserLocalNow(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId) || timeZoneId == "UTC")
            return DateTime.UtcNow;

        var timeZone = ResolveTimeZone(timeZoneId);
        return timeZone == null ? DateTime.UtcNow : TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
    }

    private static TimeZoneInfo? ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch
        {
            if (TryResolveIanaToWindows(timeZoneId, out var windowsId))
                return TimeZoneInfo.FindSystemTimeZoneById(windowsId);

            if (TryResolveWindowsToIana(timeZoneId, out var ianaId))
                return TimeZoneInfo.FindSystemTimeZoneById(ianaId);

            return null;
        }
    }

    private static bool TryResolveIanaToWindows(string timeZoneId, out string windowsId)
    {
        windowsId = string.Empty;
        try
        {
            return TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out windowsId);
        }
        catch (Exception ex) when (IsMissingTimeZoneMapping(ex))
        {
            return false;
        }
    }

    private static bool TryResolveWindowsToIana(string timeZoneId, out string ianaId)
    {
        ianaId = string.Empty;
        try
        {
            return TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneId, out ianaId);
        }
        catch (Exception ex) when (IsMissingTimeZoneMapping(ex))
        {
            return false;
        }
    }

    private static bool IsMissingTimeZoneMapping(Exception exception)
    {
        return exception is InvalidTimeZoneException || exception is TimeZoneNotFoundException;
    }
}
