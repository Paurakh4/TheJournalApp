using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reflecta.Models;

/// <summary>
/// Junction table for Many-to-Many relationship between JournalEntry and Tag
/// </summary>
[Table("entry_tags")]
public class EntryTag
{
    [Column("entry_id")]
    public Guid EntryId { get; set; }

    [Column("tag_id")]
    public Guid TagId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("EntryId")]
    public virtual JournalEntry Entry { get; set; } = null!;

    [ForeignKey("TagId")]
    public virtual Tag Tag { get; set; } = null!;
}
