using Microsoft.EntityFrameworkCore;
using TheJournalApp.Data;
using TheJournalApp.Data.Entities;

namespace TheJournalApp.Services;

public class JournalService
{
    private readonly JournalDbContext _context;
    private int _currentUserId = 1;

    public JournalService(JournalDbContext context)
    {
        _context = context;
    }

    public async Task EnsureUserExistsAsync()
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == _currentUserId);
        if (user == null)
        {
            user = new User
            {
                Username = "User",
                PasswordHash = "demo",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _currentUserId = user.UserId;

            _context.Streaks.Add(new Streak { UserId = user.UserId });
            _context.UserSettings.Add(new UserSettings { UserId = user.UserId });
            await _context.SaveChangesAsync();
        }
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        return await _context.Users
            .Include(u => u.Settings)
            .Include(u => u.Streak)
            .FirstOrDefaultAsync(u => u.UserId == _currentUserId);
    }

    public async Task<List<Mood>> GetAllMoodsAsync()
    {
        return await _context.Moods.OrderBy(m => m.MoodId).ToListAsync();
    }

    public async Task<List<Tag>> GetAvailableTagsAsync()
    {
        return await _context.Tags
            .Where(t => t.UserId == null || t.UserId == _currentUserId)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<JournalEntry?> GetTodayEntryAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return await _context.JournalEntries
            .Include(e => e.PrimaryMood)
            .Include(e => e.SecondaryMoods).ThenInclude(sm => sm.Mood)
            .Include(e => e.EntryTags).ThenInclude(et => et.Tag)
            .FirstOrDefaultAsync(e => e.UserId == _currentUserId && e.EntryDate == today);
    }

    public async Task<JournalEntry?> GetEntryByIdAsync(int entryId)
    {
        return await _context.JournalEntries
            .Include(e => e.PrimaryMood)
            .Include(e => e.SecondaryMoods).ThenInclude(sm => sm.Mood)
            .Include(e => e.EntryTags).ThenInclude(et => et.Tag)
            .FirstOrDefaultAsync(e => e.EntryId == entryId && e.UserId == _currentUserId);
    }

    public async Task<JournalEntry?> GetEntryByDateAsync(DateOnly date)
    {
        return await _context.JournalEntries
            .Include(e => e.PrimaryMood)
            .FirstOrDefaultAsync(e => e.UserId == _currentUserId && e.EntryDate == date);
    }

    public async Task<List<JournalEntry>> GetEntriesAsync(string? searchQuery = null, int? moodId = null, int? tagId = null)
    {
        var query = _context.JournalEntries
            .Include(e => e.PrimaryMood)
            .Include(e => e.EntryTags).ThenInclude(et => et.Tag)
            .Where(e => e.UserId == _currentUserId);

        if (!string.IsNullOrWhiteSpace(searchQuery))
            query = query.Where(e => e.Content.ToLower().Contains(searchQuery.ToLower()));

        if (moodId.HasValue)
            query = query.Where(e => e.PrimaryMoodId == moodId.Value);

        if (tagId.HasValue)
            query = query.Where(e => e.EntryTags.Any(et => et.TagId == tagId.Value));

        return await query.OrderByDescending(e => e.EntryDate).ToListAsync();
    }

    public async Task<List<JournalEntry>> GetEntriesForMonthAsync(int year, int month)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await _context.JournalEntries
            .Include(e => e.PrimaryMood)
            .Where(e => e.UserId == _currentUserId && e.EntryDate >= startDate && e.EntryDate <= endDate)
            .ToListAsync();
    }

    public async Task<int> GetTotalEntriesCountAsync()
    {
        return await _context.JournalEntries.CountAsync(e => e.UserId == _currentUserId);
    }

    public async Task<int> GetThisWeekEntriesCountAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        return await _context.JournalEntries
            .CountAsync(e => e.UserId == _currentUserId && e.EntryDate >= startOfWeek);
    }

    public async Task<Streak?> GetStreakAsync()
    {
        return await _context.Streaks.FirstOrDefaultAsync(s => s.UserId == _currentUserId);
    }

    public async Task<string> GetDominantMoodEmojiAsync()
    {
        var moodCounts = await _context.JournalEntries
            .Where(e => e.UserId == _currentUserId)
            .GroupBy(e => e.PrimaryMoodId)
            .Select(g => new { MoodId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefaultAsync();

        if (moodCounts == null) return "😐";

        var mood = await _context.Moods.FindAsync(moodCounts.MoodId);
        return mood?.Emoji ?? "😐";
    }

    public async Task<JournalEntry> SaveEntryAsync(int? entryId, int primaryMoodId, string content, 
        List<int> secondaryMoodIds, List<int> tagIds, DateOnly entryDate)
    {
        JournalEntry entry;

        if (entryId.HasValue)
        {
            entry = await _context.JournalEntries
                .Include(e => e.SecondaryMoods)
                .Include(e => e.EntryTags)
                .FirstAsync(e => e.EntryId == entryId.Value);

            entry.PrimaryMoodId = primaryMoodId;
            entry.Content = content;
            entry.WordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            entry.UpdatedAt = DateTime.UtcNow;

            _context.EntrySecondaryMoods.RemoveRange(entry.SecondaryMoods);
            _context.EntryTags.RemoveRange(entry.EntryTags);
        }
        else
        {
            entry = new JournalEntry
            {
                UserId = _currentUserId,
                PrimaryMoodId = primaryMoodId,
                Content = content,
                WordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                EntryDate = entryDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.JournalEntries.Add(entry);
        }

        await _context.SaveChangesAsync();

        foreach (var moodId in secondaryMoodIds.Take(2))
        {
            _context.EntrySecondaryMoods.Add(new EntrySecondaryMood { EntryId = entry.EntryId, MoodId = moodId });
        }

        foreach (var tagId in tagIds)
        {
            _context.EntryTags.Add(new EntryTag { EntryId = entry.EntryId, TagId = tagId });
        }

        await _context.SaveChangesAsync();
        await UpdateStreakAsync();

        return entry;
    }

    public async Task DeleteEntryAsync(int entryId)
    {
        var entry = await _context.JournalEntries.FindAsync(entryId);
        if (entry != null && entry.UserId == _currentUserId)
        {
            _context.JournalEntries.Remove(entry);
            await _context.SaveChangesAsync();
            await UpdateStreakAsync();
        }
    }

    public async Task<Tag> CreateTagAsync(string name)
    {
        var tag = new Tag { UserId = _currentUserId, Name = name, IsBuiltin = false };
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    private async Task UpdateStreakAsync()
    {
        var streak = await _context.Streaks.FirstOrDefaultAsync(s => s.UserId == _currentUserId);
        if (streak == null) return;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var entries = await _context.JournalEntries
            .Where(e => e.UserId == _currentUserId)
            .Select(e => e.EntryDate)
            .OrderByDescending(d => d)
            .ToListAsync();

        if (!entries.Any())
        {
            streak.CurrentStreak = 0;
            streak.LastEntryDate = null;
        }
        else
        {
            int currentStreak = 0;
            var checkDate = today;

            foreach (var date in entries.Distinct().OrderByDescending(d => d))
            {
                if (date == checkDate || date == checkDate.AddDays(-1))
                {
                    currentStreak++;
                    checkDate = date;
                }
                else if (date < checkDate.AddDays(-1))
                    break;
            }

            streak.CurrentStreak = currentStreak;
            streak.LastEntryDate = entries.First();

            if (currentStreak > streak.LongestStreak)
                streak.LongestStreak = currentStreak;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<AnalyticsData> GetAnalyticsAsync()
    {
        var streak = await GetStreakAsync();
        var entries = await _context.JournalEntries
            .Include(e => e.PrimaryMood)
            .Include(e => e.EntryTags).ThenInclude(et => et.Tag)
            .Where(e => e.UserId == _currentUserId)
            .ToListAsync();

        var thisMonth = DateTime.Today.Month;
        var thisYear = DateTime.Today.Year;
        var entriesThisMonth = entries.Where(e => e.EntryDate.Month == thisMonth && e.EntryDate.Year == thisYear).ToList();

        var moodDistribution = entries
            .GroupBy(e => e.PrimaryMood)
            .Select(g => new MoodDistributionItem
            {
                Emoji = g.Key.Emoji,
                Label = g.Key.Name,
                Category = g.Key.Category,
                Count = g.Count(),
                Percentage = entries.Count > 0 ? (int)Math.Round(g.Count() * 100.0 / entries.Count) : 0
            })
            .OrderByDescending(m => m.Count)
            .ToList();

        var topTags = entries
            .SelectMany(e => e.EntryTags)
            .GroupBy(et => et.Tag)
            .Select(g => new TagStatItem { Name = g.Key.Name, Count = g.Count() })
            .OrderByDescending(t => t.Count)
            .Take(5)
            .ToList();

        var maxTagCount = topTags.FirstOrDefault()?.Count ?? 1;
        foreach (var tag in topTags)
            tag.Percentage = (int)Math.Round(tag.Count * 100.0 / maxTagCount);

        var weeklyActivity = new List<DayActivityItem>();
        for (int i = 6; i >= 0; i--)
        {
            var date = DateOnly.FromDateTime(DateTime.Today.AddDays(-i));
            var dayEntry = entries.FirstOrDefault(e => e.EntryDate == date);
            weeklyActivity.Add(new DayActivityItem
            {
                DayLabel = date.DayOfWeek.ToString()[..3],
                WordCount = dayEntry?.WordCount ?? 0
            });
        }

        return new AnalyticsData
        {
            CurrentStreak = streak?.CurrentStreak ?? 0,
            LongestStreak = streak?.LongestStreak ?? 0,
            TotalEntries = entries.Count,
            AverageWordsPerEntry = entries.Count > 0 ? (int)entries.Average(e => e.WordCount) : 0,
            EntriesThisMonth = entriesThisMonth.Count,
            DaysJournaledThisMonth = entriesThisMonth.Select(e => e.EntryDate).Distinct().Count(),
            CompletionRate = (int)Math.Round(entriesThisMonth.Select(e => e.EntryDate).Distinct().Count() * 100.0 / DateTime.DaysInMonth(thisYear, thisMonth)),
            MoodDistribution = moodDistribution,
            TopTags = topTags,
            WeeklyActivity = weeklyActivity
        };
    }

    public async Task<UserSettings?> GetUserSettingsAsync()
    {
        return await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == _currentUserId);
    }

    public async Task SaveUserSettingsAsync(bool isDarkMode, string? appPin)
    {
        var settings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == _currentUserId);
        if (settings != null)
        {
            settings.IsDarkMode = isDarkMode;
            if (appPin != null) settings.AppPin = appPin;
            settings.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAllDataAsync()
    {
        var entries = await _context.JournalEntries.Where(e => e.UserId == _currentUserId).ToListAsync();
        _context.JournalEntries.RemoveRange(entries);

        var streak = await _context.Streaks.FirstOrDefaultAsync(s => s.UserId == _currentUserId);
        if (streak != null)
        {
            streak.CurrentStreak = 0;
            streak.LongestStreak = 0;
            streak.LastEntryDate = null;
        }

        await _context.SaveChangesAsync();
    }
}

public class AnalyticsData
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int TotalEntries { get; set; }
    public int AverageWordsPerEntry { get; set; }
    public int EntriesThisMonth { get; set; }
    public int DaysJournaledThisMonth { get; set; }
    public int CompletionRate { get; set; }
    public List<MoodDistributionItem> MoodDistribution { get; set; } = new();
    public List<TagStatItem> TopTags { get; set; } = new();
    public List<DayActivityItem> WeeklyActivity { get; set; } = new();
}

public class MoodDistributionItem
{
    public string Emoji { get; set; } = "";
    public string Label { get; set; } = "";
    public MoodCategory Category { get; set; }
    public int Count { get; set; }
    public int Percentage { get; set; }
    public string ColorClass => Category switch
    {
        MoodCategory.Positive => "bar-happy",
        MoodCategory.Neutral => "bar-neutral",
        MoodCategory.Negative => "bar-sad",
        _ => ""
    };
}

public class TagStatItem
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public int Percentage { get; set; }
}

public class DayActivityItem
{
    public string DayLabel { get; set; } = "";
    public int WordCount { get; set; }
}
