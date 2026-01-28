using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reflecta.Data.Models;

public enum ExportFormat
{
    PDF,
    Markdown,
    PlainText
}

[Table("export_logs")]
public class ExportLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("export_format")]
    public ExportFormat ExportFormat { get; set; }

    [Required]
    [Column("date_range_start")]
    public DateOnly DateRangeStart { get; set; }

    [Required]
    [Column("date_range_end")]
    public DateOnly DateRangeEnd { get; set; }

    [Column("entries_count")]
    public int EntriesCount { get; set; }

    [Column("file_size_bytes")]
    public long? FileSizeBytes { get; set; }

    [Column("exported_at")]
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
