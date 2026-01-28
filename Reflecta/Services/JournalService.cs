using Microsoft.EntityFrameworkCore;
using Reflecta.Data;
using Reflecta.Models;
using Reflecta.Services.Interfaces;

namespace Reflecta.Services;

public class JournalService : IJournalService
{
    private readonly IDbContextFactory<ReflectaDbContext> _contextFactory;
    private readonly IStreakService _streakService;
    private readonly ISettingsService _settingsService;

    public JournalService(IDbContextFactory<ReflectaDbContext> contextFactory, IStreakService streakService, ISettingsService settingsService)
    {
        _contextFactory = contextFactory;
        _streakService = streakService;
        _settingsService = settingsService;
    }

    public async Task<(bool Success, string Message, JournalEntry? Entry)> CreateEntryAsync(Guid userId, CreateEntryDto dto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var timeZoneId = await _settingsService.GetTimeZoneAsync(userId);
        var today = DateOnly.FromDateTime(GetUserLocalNow(timeZoneId));

        // Check if entry already exists for today
        var existingEntry = await context.JournalEntries
            .FirstOrDefaultAsync(e => e.UserId == userId && e.EntryDate == today);

        if (existingEntry != null)
            return (false, "An entry already exists for today. You can edit the existing entry instead.", null);

        // Validate primary mood
        var moodExists = await context.Moods.AnyAsync(m => m.Id == dto.PrimaryMoodId && m.IsActive);
        if (!moodExists)
            return (false, "Invalid mood selected", null);

        // Validate secondary moods (max 2)
        if (dto.SecondaryMoodIds.Count > 2)
            return (false, "Maximum 2 secondary moods allowed", null);

        // Calculate word count
        var wordCount = CalculateWordCount(dto.Content);

        var entry = new JournalEntry
        {
            UserId = userId,
            EntryDate = today,
            Title = dto.Title,
            Content = dto.Content,
            PrimaryMoodId = dto.PrimaryMoodId,
            WordCount = wordCount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.JournalEntries.Add(entry);
        await context.SaveChangesAsync();

        // Add secondary moods
        foreach (var moodId in dto.SecondaryMoodIds.Distinct().Take(2))
        {
            if (moodId != dto.PrimaryMoodId)
            {
                context.EntrySecondaryMoods.Add(new EntrySecondaryMood
                {
                    EntryId = entry.Id,
                    MoodId = moodId
                });
            }
        }

        // Add tags
        foreach (var tagId in dto.TagIds.Distinct())
        {
            var tagExists = await context.Tags.AnyAsync(t => t.Id == tagId && t.IsActive);
            if (tagExists)
            {
                context.EntryTags.Add(new EntryTag
                {
                    EntryId = entry.Id,
                    TagId = tagId
                });
            }
        }

        await context.SaveChangesAsync();

        // Update streak
        await _streakService.UpdateStreakAsync(userId, timeZoneId);

        // Load full entry with relations
        var fullEntry = await GetEntryByIdAsync(entry.Id, userId);
        return (true, "Entry created successfully", fullEntry);
    }

    public async Task<(bool Success, string Message, JournalEntry? Entry)> UpdateEntryAsync(
        Guid entryId, Guid userId, UpdateEntryDto dto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entry = await context.JournalEntries
            .Include(e => e.EntryTags)
            .Include(e => e.SecondaryMoods)
            .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId);

        if (entry == null)
            return (false, "Entry not found", null);

        // Validate primary mood
        var moodExists = await context.Moods.AnyAsync(m => m.Id == dto.PrimaryMoodId && m.IsActive);
        if (!moodExists)
            return (false, "Invalid mood selected", null);

        // Validate secondary moods (max 2)
        if (dto.SecondaryMoodIds.Count > 2)
            return (false, "Maximum 2 secondary moods allowed", null);

        // Update basic fields
        entry.Title = dto.Title;
        entry.Content = dto.Content;
        entry.PrimaryMoodId = dto.PrimaryMoodId;
        entry.WordCount = CalculateWordCount(dto.Content);
        entry.UpdatedAt = DateTime.UtcNow;

        // Update secondary moods
        context.EntrySecondaryMoods.RemoveRange(entry.SecondaryMoods);
        foreach (var moodId in dto.SecondaryMoodIds.Distinct().Take(2))
        {
            if (moodId != dto.PrimaryMoodId)
            {
                context.EntrySecondaryMoods.Add(new EntrySecondaryMood
                {
                    EntryId = entry.Id,
                    MoodId = moodId
                });
            }
        }

        // Update tags
        context.EntryTags.RemoveRange(entry.EntryTags);
        foreach (var tagId in dto.TagIds.Distinct())
        {
            var tagExists = await context.Tags.AnyAsync(t => t.Id == tagId && t.IsActive);
            if (tagExists)
            {
                context.EntryTags.Add(new EntryTag
                {
                    EntryId = entry.Id,
                    TagId = tagId
                });
            }
        }

        await context.SaveChangesAsync();

        var fullEntry = await GetEntryByIdAsync(entry.Id, userId);
        return (true, "Entry updated successfully", fullEntry);
    }

    public async Task<(bool Success, string Message)> DeleteEntryAsync(Guid entryId, Guid userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entry = await context.JournalEntries
            .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId);

        if (entry == null)
            return (false, "Entry not found");

        context.JournalEntries.Remove(entry);
        await context.SaveChangesAsync();

        // Update streak
        var timeZoneId = await _settingsService.GetTimeZoneAsync(userId);
        await _streakService.RecalculateStreakAsync(userId, timeZoneId);

        return (true, "Entry deleted successfully");
    }

    public async Task<JournalEntry?> GetEntryByIdAsync(Guid entryId, Guid userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.JournalEntries
            .Include(e => e.PrimaryMood)
            .Include(e => e.SecondaryMoods)
                .ThenInclude(sm => sm.Mood)
            .Include(e => e.EntryTags)
                .ThenInclude(et => et.Tag)
            .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId);
    }

    public async Task<JournalEntry?> GetEntryByDateAsync(Guid userId, DateOnly date)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.JournalEntries
            .Include(e => e.PrimaryMood)
            .Include(e => e.SecondaryMoods)
                .ThenInclude(sm => sm.Mood)
            .Include(e => e.EntryTags)
                .ThenInclude(et => et.Tag)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.EntryDate == date);
    }

    public async Task<bool> EntryExistsForDateAsync(Guid userId, DateOnly date)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.JournalEntries
            .AnyAsync(e => e.UserId == userId && e.EntryDate == date);
    }

    public async Task<PagedResult<JournalEntry>> GetEntriesAsync(Guid userId, EntryFilterDto filter)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.JournalEntries
            .Include(e => e.PrimaryMood)
            .Include(e => e.SecondaryMoods)
                .ThenInclude(sm => sm.Mood)
            .Include(e => e.EntryTags)
                .ThenInclude(et => et.Tag)
            .Where(e => e.UserId == userId)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var searchLower = filter.SearchText.ToLower();
            query = query.Where(e => 
                (e.Title != null && e.Title.ToLower().Contains(searchLower)) ||
                (e.Content != null && e.Content.ToLower().Contains(searchLower)));
        }

        if (filter.MoodId.HasValue)
        {
            query = query.Where(e => e.PrimaryMoodId == filter.MoodId.Value);
        }

        if (filter.TagIds != null && filter.TagIds.Any())
        {
            query = query.Where(e => e.EntryTags.Any(et => filter.TagIds.Contains(et.TagId)));
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(e => e.EntryDate >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(e => e.EntryDate <= filter.EndDate.Value);
        }

        if (filter.IsFavorite.HasValue)
        {
            query = query.Where(e => e.IsFavorite == filter.IsFavorite.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var items = await query
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<JournalEntry>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<List<JournalEntry>> GetRecentEntriesAsync(Guid userId, int count = 5)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.JournalEntries
            .Include(e => e.PrimaryMood)
            .Include(e => e.EntryTags)
                .ThenInclude(et => et.Tag)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<(bool Success, bool IsFavorite)> ToggleFavoriteAsync(Guid entryId, Guid userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entry = await context.JournalEntries
            .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId);

        if (entry == null)
            return (false, false);

        entry.IsFavorite = !entry.IsFavorite;
        entry.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return (true, entry.IsFavorite);
    }

    public async Task<int> GetTotalEntryCountAsync(Guid userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.JournalEntries.CountAsync(e => e.UserId == userId);
    }

    public async Task<int> GetTotalWordCountAsync(Guid userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.JournalEntries
            .Where(e => e.UserId == userId)
            .SumAsync(e => e.WordCount);
    }

    public async Task<List<JournalEntry>> GetEntriesForMonthAsync(Guid userId, int year, int month)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.JournalEntries
            .Include(e => e.PrimaryMood)
            .Where(e => e.UserId == userId && e.EntryDate >= startDate && e.EntryDate <= endDate)
            .OrderBy(e => e.EntryDate)
            .ToListAsync();
    }

    private static int CalculateWordCount(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        return content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
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
