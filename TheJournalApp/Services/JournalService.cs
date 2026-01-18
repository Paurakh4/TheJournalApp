using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TheJournalApp.Data;
using TheJournalApp.Data.Entities;

namespace TheJournalApp.Services;

public class JournalService
{
    private readonly JournalDbContext _context;
    private int? _currentUserId = null;

    public event Action? OnAuthStateChanged;

    public bool IsAuthenticated => _currentUserId.HasValue;

    public JournalService(JournalDbContext context)
    {
        _context = context;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null) return false;

        var hash = HashPassword(password);
        if (user.PasswordHash != hash) return false;

        _currentUserId = user.UserId;
        await RecalculateStreakAsync();
        OnAuthStateChanged?.Invoke();
        return true;
    }

    public async Task<bool> RegisterAsync(string username, string password)
    {
        if (await _context.Users.AnyAsync(u => u.Username == username))
            return false;

        var user = new User
        {
            Username = username,
            PasswordHash = HashPassword(password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _currentUserId = user.UserId;

        // Initialize user data
        _context.Streaks.Add(new Streak { UserId = user.UserId });
        _context.UserSettings.Add(new UserSettings { UserId = user.UserId });
        await _context.SaveChangesAsync();

        OnAuthStateChanged?.Invoke();
        return true;
    }

    public void Logout()
    {
        _currentUserId = null;
        OnAuthStateChanged?.Invoke();
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    // EnsureSeedDataAsync method removed as it is no longer needed


    public async Task<User?> GetCurrentUserAsync()
    {
        if (!_currentUserId.HasValue) return null;
        return await _context.Users
            .Include(u => u.Settings)
            .Include(u => u.Streak)
            .FirstOrDefaultAsync(u => u.UserId == _currentUserId.Value);
    }

    public async Task<List<Mood>> GetAllMoodsAsync()
    {
        return await _context.Moods.OrderBy(m => m.MoodId).ToListAsync();
    }

    public async Task<List<Tag>> GetAvailableTagsAsync()
    {
        if (!_currentUserId.HasValue) return new List<Tag>();
        return await _context.Tags
            .Where(t => t.UserId == null || t.UserId == _currentUserId.Value)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<JournalEntry?> GetTodayEntryAsync()
    {
        if (!_currentUserId.HasValue) return null;
        var today = DateOnly.FromDateTime(DateTime.Today);
        return await _context.JournalEntries
            .Include(e => e.PrimaryMood)
            .Include(e => e.SecondaryMoods).ThenInclude(sm => sm.Mood)
            .Include(e => e.EntryTags).ThenInclude(et => et.Tag)
            .FirstOrDefaultAsync(e => e.UserId == _currentUserId.Value && e.EntryDate == today);
    }

    public async Task<JournalEntry?> GetEntryByIdAsync(int entryId)
    {
        if (!_currentUserId.HasValue) return null;
        return await _context.JournalEntries
            .Include(e => e.PrimaryMood)
            .Include(e => e.SecondaryMoods).ThenInclude(sm => sm.Mood)
            .Include(e => e.EntryTags).ThenInclude(et => et.Tag)
            .FirstOrDefaultAsync(e => e.EntryId == entryId && e.UserId == _currentUserId.Value);
    }

    public async Task<JournalEntry?> GetEntryByDateAsync(DateOnly date)
    {
        if (!_currentUserId.HasValue) return null;
        return await _context.JournalEntries
            .Include(e => e.PrimaryMood)
            .FirstOrDefaultAsync(e => e.UserId == _currentUserId.Value && e.EntryDate == date);
    }

    public async Task<List<JournalEntry>> GetEntriesAsync(string? searchQuery = null, int? moodId = null, int? tagId = null, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        if (!_currentUserId.HasValue) return new List<JournalEntry>();
        var query = _context.JournalEntries
            .Include(e => e.PrimaryMood)
            .Include(e => e.EntryTags).ThenInclude(et => et.Tag)
            .Where(e => e.UserId == _currentUserId.Value);

        if (!string.IsNullOrWhiteSpace(searchQuery))
            query = query.Where(e => e.Content.ToLower().Contains(searchQuery.ToLower()));

        if (moodId.HasValue)
            query = query.Where(e => e.PrimaryMoodId == moodId.Value);

        if (tagId.HasValue)
            query = query.Where(e => e.EntryTags.Any(et => et.TagId == tagId.Value));

        if (startDate.HasValue)
            query = query.Where(e => e.EntryDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.EntryDate <= endDate.Value);

        return await query.OrderByDescending(e => e.EntryDate).ToListAsync();
    }

    public async Task<List<JournalEntry>> GetEntriesForMonthAsync(int year, int month)
    {
        if (!_currentUserId.HasValue) return new List<JournalEntry>();
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await _context.JournalEntries
            .Include(e => e.PrimaryMood)
            .Where(e => e.UserId == _currentUserId.Value && e.EntryDate >= startDate && e.EntryDate <= endDate)
            .ToListAsync();
    }

    public async Task<List<JournalEntry>> GetRecentEntriesAsync(int count = 5)
    {
        if (!_currentUserId.HasValue) return new List<JournalEntry>();
        return await _context.JournalEntries
            .Include(e => e.PrimaryMood)
            .Include(e => e.EntryTags).ThenInclude(et => et.Tag)
            .Where(e => e.UserId == _currentUserId.Value)
            .OrderByDescending(e => e.EntryDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> GetTotalEntriesCountAsync()
    {
        if (!_currentUserId.HasValue) return 0;
        return await _context.JournalEntries.CountAsync(e => e.UserId == _currentUserId.Value);
    }

    public async Task<int> GetThisWeekEntriesCountAsync()
    {
        if (!_currentUserId.HasValue) return 0;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        return await _context.JournalEntries
            .CountAsync(e => e.UserId == _currentUserId.Value && e.EntryDate >= startOfWeek);
    }

    public async Task<Streak?> GetStreakAsync()
    {
        if (!_currentUserId.HasValue) return null;
        return await _context.Streaks.FirstOrDefaultAsync(s => s.UserId == _currentUserId.Value);
    }

    public async Task RecalculateStreakAsync()
    {
        if (!_currentUserId.HasValue) return;
        var streak = await _context.Streaks.FirstOrDefaultAsync(s => s.UserId == _currentUserId.Value);
        if (streak == null) return;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var entries = await _context.JournalEntries
            .Where(e => e.UserId == _currentUserId.Value)
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

    public async Task<string> GetDominantMoodEmojiAsync()
    {
        if (!_currentUserId.HasValue) return "😐";
        var moodCounts = await _context.JournalEntries
            .Where(e => e.UserId == _currentUserId.Value)
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
        if (!_currentUserId.HasValue) throw new InvalidOperationException("User not authenticated");
        JournalEntry entry;

        // Check if an entry already exists for this date if we're trying to create a new one
        if (!entryId.HasValue)
        {
            var existingEntry = await _context.JournalEntries
                .FirstOrDefaultAsync(e => e.UserId == _currentUserId.Value && e.EntryDate == entryDate);
            
            if (existingEntry != null)
            {
                entryId = existingEntry.EntryId;
            }
        }

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
                UserId = _currentUserId.Value,
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
        if (!_currentUserId.HasValue) return;
        var entry = await _context.JournalEntries.FindAsync(entryId);
        if (entry != null && entry.UserId == _currentUserId.Value)
        {
            _context.JournalEntries.Remove(entry);
            await _context.SaveChangesAsync();
            await UpdateStreakAsync();
        }
    }

    public async Task<Tag> CreateTagAsync(string name)
    {
        if (!_currentUserId.HasValue) throw new InvalidOperationException("User not authenticated");
        var tag = new Tag { UserId = _currentUserId.Value, Name = name, IsBuiltin = false };
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    private async Task UpdateStreakAsync()
    {
        await RecalculateStreakAsync();
    }

    public async Task<AnalyticsData> GetAnalyticsAsync()
    {
        if (!_currentUserId.HasValue) return new AnalyticsData();
        var streak = await GetStreakAsync();
        var entries = await _context.JournalEntries
            .Include(e => e.PrimaryMood)
            .Include(e => e.EntryTags).ThenInclude(et => et.Tag)
            .Where(e => e.UserId == _currentUserId.Value)
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
        if (!_currentUserId.HasValue) return null;
        return await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == _currentUserId.Value);
    }

    public async Task SaveUserSettingsAsync(bool isDarkMode, string? appPin, bool requirePinOnLaunch)
    {
        if (!_currentUserId.HasValue) return;
        var settings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == _currentUserId.Value);
        if (settings != null)
        {
            settings.IsDarkMode = isDarkMode;
            if (appPin != null) settings.AppPin = appPin;
            settings.RequirePinOnLaunch = requirePinOnLaunch;
            settings.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task InitializeDatabaseAsync()
    {
        try 
        {
            // Ensure the new column exists
            await _context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"UserSettings\" ADD COLUMN IF NOT EXISTS \"RequirePinOnLaunch\" BOOLEAN DEFAULT FALSE;");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating schema: {ex.Message}");
        }
    }

    public async Task DeleteAllDataAsync()
    {
        if (!_currentUserId.HasValue) return;
        var entries = await _context.JournalEntries.Where(e => e.UserId == _currentUserId.Value).ToListAsync();
        _context.JournalEntries.RemoveRange(entries);

        var streak = await _context.Streaks.FirstOrDefaultAsync(s => s.UserId == _currentUserId.Value);
        if (streak != null)
        {
            streak.CurrentStreak = 0;
            streak.LongestStreak = 0;
            streak.LastEntryDate = null;
        }

        await _context.SaveChangesAsync();
    }

    public async Task LogExportAsync(DateOnly startDate, DateOnly endDate, string format = "PDF")
    {
        if (!_currentUserId.HasValue) return;

        var log = new ExportLog
        {
            UserId = _currentUserId.Value,
            RangeStart = startDate,
            RangeEnd = endDate,
            Format = format,
            ExportedAt = DateTime.UtcNow
        };

        _context.ExportLogs.Add(log);
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
