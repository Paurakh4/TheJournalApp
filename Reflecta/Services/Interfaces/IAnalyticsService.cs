using Reflecta.Models;

namespace Reflecta.Services.Interfaces;

public class MoodDistributionData
{
    public Mood Mood { get; set; } = null!;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class WeeklyActivityData
{
    public DayOfWeek DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public int EntryCount { get; set; }
    public int TotalWordCount { get; set; }
}

public class MonthlyOverviewData
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int EntryCount { get; set; }
    public int TotalWordCount { get; set; }
    public Mood? MostFrequentMood { get; set; }
}

public class WordCountTrendData
{
    public DateOnly Date { get; set; }
    public int WordCount { get; set; }
}

public class AnalyticsSummary
{
    public int TotalEntries { get; set; }
    public int TotalWords { get; set; }
    public int DaysActive { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public double AverageWordsPerEntry { get; set; }
    public Mood? MostFrequentMood { get; set; }
    public List<MoodDistributionData> MoodDistribution { get; set; } = new();
    public List<WeeklyActivityData> WeeklyActivity { get; set; } = new();
    public List<(Tag Tag, int Count)> TopTags { get; set; } = new();
    public int MissedDays { get; set; }
    public List<DateOnly> RecentMissedDates { get; set; } = new();
    public List<TagCategoryBreakdownData> TagBreakdown { get; set; } = new();
    public string TimeZoneId { get; set; } = "UTC";
}

public class TagCategoryBreakdownData
{
    public string Category { get; set; } = string.Empty;
    public int EntryCount { get; set; }
    public double Percentage { get; set; }
}

public interface IAnalyticsService
{
    /// <summary>
    /// Get comprehensive analytics summary with optional date range filtering
    /// </summary>
    Task<AnalyticsSummary> GetAnalyticsSummaryAsync(Guid userId, DateOnly? startDate = null, DateOnly? endDate = null);

    /// <summary>
    /// Get mood distribution for a date range
    /// </summary>
    Task<List<MoodDistributionData>> GetMoodDistributionAsync(Guid userId, DateOnly? startDate = null, DateOnly? endDate = null);

    /// <summary>
    /// Get the most frequent mood
    /// </summary>
    Task<Mood?> GetMostFrequentMoodAsync(Guid userId, DateOnly? startDate = null, DateOnly? endDate = null);

    /// <summary>
    /// Get weekly activity data
    /// </summary>
    Task<List<WeeklyActivityData>> GetWeeklyActivityAsync(Guid userId, int weeksBack = 4);

    /// <summary>
    /// Get monthly overview for past months
    /// </summary>
    Task<List<MonthlyOverviewData>> GetMonthlyOverviewAsync(Guid userId, int monthsBack = 6);

    /// <summary>
    /// Get word count trend data
    /// </summary>
    Task<List<WordCountTrendData>> GetWordCountTrendAsync(Guid userId, int daysBack = 30);

    /// <summary>
    /// Get top tags with usage count
    /// </summary>
    Task<List<(Tag Tag, int Count)>> GetTopTagsAsync(Guid userId, int count = 5, DateOnly? startDate = null, DateOnly? endDate = null);

    /// <summary>
    /// Get average words per entry
    /// </summary>
    Task<double> GetAverageWordsPerEntryAsync(Guid userId, DateOnly? startDate = null, DateOnly? endDate = null);

    /// <summary>
    /// Get tag breakdown by category
    /// </summary>
    Task<List<TagCategoryBreakdownData>> GetTagBreakdownByCategoryAsync(Guid userId, DateOnly? startDate = null, DateOnly? endDate = null);

    /// <summary>
    /// Get missed days in a date range
    /// </summary>
    Task<List<DateOnly>> GetMissedDaysAsync(Guid userId, DateOnly startDate, DateOnly endDate);
}
