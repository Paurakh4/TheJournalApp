using Reflecta.Models;

namespace Reflecta.Services.Interfaces;

public class CreateEntryDto
{
    public string? Title { get; set; }
    public required string Content { get; set; }
    public required int PrimaryMoodId { get; set; }
    public List<int> SecondaryMoodIds { get; set; } = new();
    public List<Guid> TagIds { get; set; } = new();
}

public class UpdateEntryDto
{
    public string? Title { get; set; }
    public required string Content { get; set; }
    public required int PrimaryMoodId { get; set; }
    public List<int> SecondaryMoodIds { get; set; } = new();
    public List<Guid> TagIds { get; set; } = new();
}

public class EntryFilterDto
{
    public string? SearchText { get; set; }
    public int? MoodId { get; set; }
    public List<Guid>? TagIds { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool? IsFavorite { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public interface IJournalService
{
    /// <summary>
    /// Create a new journal entry (enforces one entry per day)
    /// </summary>
    Task<(bool Success, string Message, JournalEntry? Entry)> CreateEntryAsync(Guid userId, CreateEntryDto dto);

    /// <summary>
    /// Update an existing journal entry
    /// </summary>
    Task<(bool Success, string Message, JournalEntry? Entry)> UpdateEntryAsync(Guid entryId, Guid userId, UpdateEntryDto dto);

    /// <summary>
    /// Delete a journal entry
    /// </summary>
    Task<(bool Success, string Message)> DeleteEntryAsync(Guid entryId, Guid userId);

    /// <summary>
    /// Get a single entry by ID
    /// </summary>
    Task<JournalEntry?> GetEntryByIdAsync(Guid entryId, Guid userId);

    /// <summary>
    /// Get entry for a specific date
    /// </summary>
    Task<JournalEntry?> GetEntryByDateAsync(Guid userId, DateOnly date);

    /// <summary>
    /// Check if entry exists for a specific date
    /// </summary>
    Task<bool> EntryExistsForDateAsync(Guid userId, DateOnly date);

    /// <summary>
    /// Get paginated entries with filters
    /// </summary>
    Task<PagedResult<JournalEntry>> GetEntriesAsync(Guid userId, EntryFilterDto filter);

    /// <summary>
    /// Get recent entries (for dashboard)
    /// </summary>
    Task<List<JournalEntry>> GetRecentEntriesAsync(Guid userId, int count = 5);

    /// <summary>
    /// Toggle favorite status
    /// </summary>
    Task<(bool Success, bool IsFavorite)> ToggleFavoriteAsync(Guid entryId, Guid userId);

    /// <summary>
    /// Get total entry count for user
    /// </summary>
    Task<int> GetTotalEntryCountAsync(Guid userId);

    /// <summary>
    /// Get total word count for user
    /// </summary>
    Task<int> GetTotalWordCountAsync(Guid userId);

    /// <summary>
    /// Get entries for calendar view (specific month)
    /// </summary>
    Task<List<JournalEntry>> GetEntriesForMonthAsync(Guid userId, int year, int month);
}
