using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reflecta.Models;

/// <summary>
/// Junction table for Many-to-Many relationship between JournalEntry and Secondary Moods
/// Maximum of 2 secondary moods per entry (enforced at service level)
/// </summary>
[Table("entry_secondary_moods")]
public class EntrySecondaryMood
{
    [Column("entry_id")]
    public Guid EntryId { get; set; }

    [Column("mood_id")]
    public int MoodId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("EntryId")]
    public virtual JournalEntry Entry { get; set; } = null!;

    [ForeignKey("MoodId")]
    public virtual Mood Mood { get; set; } = null!;
}
