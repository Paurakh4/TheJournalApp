using Microsoft.EntityFrameworkCore;
using Reflecta.Data;
using Reflecta.Models;
using Reflecta.Services.Interfaces;

namespace Reflecta.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IDbContextFactory<ReflectaDbContext> _contextFactory;
    private readonly IStreakService _streakService;
    private readonly ITagService _tagService;
    private readonly ISettingsService _settingsService;

    public AnalyticsService(IDbContextFactory<ReflectaDbContext> contextFactory, IStreakService streakService, ITagService tagService, ISettingsService settingsService)
    {
        _contextFactory = contextFactory;
        _streakService = streakService;
        _tagService = tagService;
        _settingsService = settingsService;
    }

    public async Task<AnalyticsSummary> GetAnalyticsSummaryAsync(Guid userId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var streak = await _streakService.GetStreakAsync(userId);
        var timeZoneId = await _settingsService.GetTimeZoneAsync(userId);
        
        // Build base query with date filtering
        var query = context.JournalEntries.Where(e => e.UserId == userId);
        if (startDate.HasValue)
            query = query.Where(e => e.EntryDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(e => e.EntryDate <= endDate.Value);
        
        var totalEntries = await query.CountAsync();
        var totalWords = await query.SumAsync(e => e.WordCount);
        var daysActive = await _streakService.GetDaysActiveAsync(userId);
        var moodDistribution = await GetMoodDistributionAsync(userId, startDate, endDate);
        var weeklyActivity = await GetWeeklyActivityAsync(userId);
        var topTags = await GetTopTagsAsync(userId, 5, startDate, endDate);
        var mostFrequentMood = await GetMostFrequentMoodAsync(userId, startDate, endDate);
        var avgWords = await GetAverageWordsPerEntryAsync(userId, startDate, endDate);
        var tagBreakdown = await GetTagBreakdownByCategoryAsync(userId, startDate, endDate);
        
        // Calculate missed days
        var effectiveStartDate = startDate ?? DateOnly.FromDateTime(GetUserLocalNow(timeZoneId)).AddDays(-30);
        var effectiveEndDate = endDate ?? DateOnly.FromDateTime(GetUserLocalNow(timeZoneId));
        var missedDays = await GetMissedDaysAsync(userId, effectiveStartDate, effectiveEndDate);

        return new AnalyticsSummary
        {
            TotalEntries = totalEntries,
            TotalWords = totalWords,
            DaysActive = daysActive,
            CurrentStreak = streak?.CurrentStreak ?? 0,
            LongestStreak = streak?.LongestStreak ?? 0,
            AverageWordsPerEntry = avgWords,
            MostFrequentMood = mostFrequentMood,
            MoodDistribution = moodDistribution,
            WeeklyActivity = weeklyActivity,
            TopTags = topTags,
            MissedDays = missedDays.Count,
            RecentMissedDates = missedDays.OrderByDescending(d => d).Take(5).ToList(),
            TagBreakdown = tagBreakdown,
            TimeZoneId = timeZoneId
        };
    }

    public async Task<List<MoodDistributionData>> GetMoodDistributionAsync(
        Guid userId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.JournalEntries
            .Where(e => e.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(e => e.EntryDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.EntryDate <= endDate.Value);

        var totalEntries = await query.CountAsync();

        if (totalEntries == 0)
            return new List<MoodDistributionData>();

        var moodCounts = await query
            .GroupBy(e => e.PrimaryMoodId)
            .Select(g => new { MoodId = g.Key, Count = g.Count() })
            .ToListAsync();

        var moodIds = moodCounts.Select(m => m.MoodId).ToList();
        var moods = await context.Moods
            .Where(m => moodIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id);

        return moodCounts
            .Where(m => moods.ContainsKey(m.MoodId))
            .Select(m => new MoodDistributionData
            {
                Mood = moods[m.MoodId],
                Count = m.Count,
                Percentage = Math.Round((double)m.Count / totalEntries * 100, 1)
            })
            .OrderByDescending(m => m.Count)
            .ToList();
    }

    public async Task<Mood?> GetMostFrequentMoodAsync(
        Guid userId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.JournalEntries
            .Where(e => e.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(e => e.EntryDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.EntryDate <= endDate.Value);

        var mostFrequentMoodId = await query
            .GroupBy(e => e.PrimaryMoodId)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefaultAsync();

        if (mostFrequentMoodId == 0)
            return null;

        return await context.Moods.FindAsync(mostFrequentMoodId);
    }

    public async Task<List<WeeklyActivityData>> GetWeeklyActivityAsync(Guid userId, int weeksBack = 4)
    {
        var timeZoneId = await _settingsService.GetTimeZoneAsync(userId);
        var endDate = DateOnly.FromDateTime(GetUserLocalNow(timeZoneId));
        var startDate = endDate.AddDays(-7 * weeksBack);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var entries = await context.JournalEntries
            .Where(e => e.UserId == userId && e.EntryDate >= startDate && e.EntryDate <= endDate)
            .ToListAsync();

        var result = new List<WeeklyActivityData>();

        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            var dayEntries = entries.Where(e => e.EntryDate.DayOfWeek == day).ToList();
            result.Add(new WeeklyActivityData
            {
                DayOfWeek = day,
                DayName = day.ToString().Substring(0, 3),
                EntryCount = dayEntries.Count,
                TotalWordCount = dayEntries.Sum(e => e.WordCount)
            });
        }

        return result;
    }

    public async Task<List<MonthlyOverviewData>> GetMonthlyOverviewAsync(Guid userId, int monthsBack = 6)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var result = new List<MonthlyOverviewData>();
        var timeZoneId = await _settingsService.GetTimeZoneAsync(userId);
        var today = GetUserLocalNow(timeZoneId);

        for (int i = 0; i < monthsBack; i++)
        {
            var targetDate = today.AddMonths(-i);
            var year = targetDate.Year;
            var month = targetDate.Month;

            var startDate = new DateOnly(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var entries = await context.JournalEntries
                .Where(e => e.UserId == userId && e.EntryDate >= startDate && e.EntryDate <= endDate)
                .ToListAsync();

            Mood? mostFrequentMood = null;
            if (entries.Any())
            {
                var mostFrequentMoodId = entries
                    .GroupBy(e => e.PrimaryMoodId)
                    .OrderByDescending(g => g.Count())
                    .First().Key;

                mostFrequentMood = await context.Moods.FindAsync(mostFrequentMoodId);
            }

            result.Add(new MonthlyOverviewData
            {
                Year = year,
                Month = month,
                MonthName = new DateTime(year, month, 1).ToString("MMMM"),
                EntryCount = entries.Count,
                TotalWordCount = entries.Sum(e => e.WordCount),
                MostFrequentMood = mostFrequentMood
            });
        }

        return result;
    }

    public async Task<List<WordCountTrendData>> GetWordCountTrendAsync(Guid userId, int daysBack = 30)
    {
        var timeZoneId = await _settingsService.GetTimeZoneAsync(userId);
        var endDate = DateOnly.FromDateTime(GetUserLocalNow(timeZoneId));
        var startDate = endDate.AddDays(-daysBack);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var entries = await context.JournalEntries
            .Where(e => e.UserId == userId && e.EntryDate >= startDate && e.EntryDate <= endDate)
            .Select(e => new { e.EntryDate, e.WordCount })
            .ToListAsync();

        var result = new List<WordCountTrendData>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var entry = entries.FirstOrDefault(e => e.EntryDate == date);
            result.Add(new WordCountTrendData
            {
                Date = date,
                WordCount = entry?.WordCount ?? 0
            });
        }

        return result;
    }

    public async Task<List<(Tag Tag, int Count)>> GetTopTagsAsync(Guid userId, int count = 5, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.EntryTags
            .Where(et => et.Entry.UserId == userId && et.Tag.IsActive);
        
        if (startDate.HasValue)
            query = query.Where(et => et.Entry.EntryDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(et => et.Entry.EntryDate <= endDate.Value);
        
        var tagUsage = await query
            .GroupBy(et => et.TagId)
            .Select(g => new { TagId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(count)
            .ToListAsync();

        var tagIds = tagUsage.Select(t => t.TagId).ToList();
        var tags = await context.Tags
            .Where(t => tagIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

        return tagUsage
            .Where(t => tags.ContainsKey(t.TagId))
            .Select(t => (tags[t.TagId], t.Count))
            .ToList();
    }

    public async Task<double> GetAverageWordsPerEntryAsync(Guid userId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.JournalEntries.Where(e => e.UserId == userId);
        
        if (startDate.HasValue)
            query = query.Where(e => e.EntryDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(e => e.EntryDate <= endDate.Value);
        
        var entries = await query.Select(e => e.WordCount).ToListAsync();

        if (!entries.Any())
            return 0;

        return Math.Round(entries.Average(), 1);
    }

    public async Task<List<TagCategoryBreakdownData>> GetTagBreakdownByCategoryAsync(Guid userId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.EntryTags
            .Where(et => et.Entry.UserId == userId && et.Tag.IsActive);
        
        if (startDate.HasValue)
            query = query.Where(et => et.Entry.EntryDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(et => et.Entry.EntryDate <= endDate.Value);
        
        // Get distinct entry IDs per category (count each entry once per category)
        var categoryEntryCounts = await query
            .GroupBy(et => new { et.Tag.Category, et.EntryId })
            .Select(g => new { Category = g.Key.Category ?? "Uncategorized", EntryId = g.Key.EntryId })
            .ToListAsync();
        
        var categoryBreakdown = categoryEntryCounts
            .GroupBy(x => x.Category)
            .Select(g => new { Category = g.Key, EntryCount = g.Select(x => x.EntryId).Distinct().Count() })
            .ToList();
        
        var totalEntries = categoryBreakdown.Sum(c => c.EntryCount);
        
        if (totalEntries == 0)
            return new List<TagCategoryBreakdownData>();
        
        return categoryBreakdown
            .Select(c => new TagCategoryBreakdownData
            {
                Category = c.Category,
                EntryCount = c.EntryCount,
                Percentage = Math.Round((double)c.EntryCount / totalEntries * 100, 1)
            })
            .OrderByDescending(c => c.EntryCount)
            .ToList();
    }

    public async Task<List<DateOnly>> GetMissedDaysAsync(Guid userId, DateOnly startDate, DateOnly endDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var timeZoneId = await _settingsService.GetTimeZoneAsync(userId);
        var today = DateOnly.FromDateTime(GetUserLocalNow(timeZoneId));
        
        // Don't include future dates or today (user might still journal today)
        if (endDate > today)
            endDate = today.AddDays(-1);
        
        if (startDate > endDate)
            return new List<DateOnly>();
        
        var entryDates = await context.JournalEntries
            .Where(e => e.UserId == userId && e.EntryDate >= startDate && e.EntryDate <= endDate)
            .Select(e => e.EntryDate)
            .Distinct()
            .ToListAsync();
        
        var entryDateSet = entryDates.ToHashSet();
        var missedDays = new List<DateOnly>();
        
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (!entryDateSet.Contains(date))
                missedDays.Add(date);
        }
        
        return missedDays;
    }

    private static DateTime GetUserLocalNow(string timeZoneId)
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
