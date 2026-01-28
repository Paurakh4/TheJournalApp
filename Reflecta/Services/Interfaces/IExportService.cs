using Reflecta.Models;

namespace Reflecta.Services.Interfaces;

public class ExportOptions
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public ExportFormat Format { get; set; } = ExportFormat.PDF;
    public bool IncludeMoods { get; set; } = true;
    public bool IncludeTags { get; set; } = true;
    public bool IncludeWordCount { get; set; } = true;
}

public class ExportResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public byte[]? FileData { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public int EntriesCount { get; set; }
}

public interface IExportService
{
    /// <summary>
    /// Export journal entries
    /// </summary>
    Task<ExportResult> ExportEntriesAsync(Guid userId, ExportOptions options);

    /// <summary>
    /// Get export history for user
    /// </summary>
    Task<List<ExportLog>> GetExportHistoryAsync(Guid userId, int count = 10);

    /// <summary>
    /// Log an export operation
    /// </summary>
    Task<ExportLog> LogExportAsync(Guid userId, ExportFormat format, DateOnly startDate, DateOnly endDate, int entriesCount, long? fileSizeBytes);
}
