using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reflecta.Models;

public enum MoodCategory
{
    Positive,
    Neutral,
    Negative
}

[Table("moods")]
public class Mood
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    [Column("emoji")]
    public string Emoji { get; set; } = string.Empty;

    [Required]
    [Column("category")]
    public MoodCategory Category { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<JournalEntry> PrimaryMoodEntries { get; set; } = new List<JournalEntry>();
    public virtual ICollection<EntrySecondaryMood> SecondaryMoodEntries { get; set; } = new List<EntrySecondaryMood>();
}
