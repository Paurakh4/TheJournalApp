using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reflecta.Models;

[Table("journal_entries")]
public class JournalEntry
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("entry_date")]
    public DateOnly EntryDate { get; set; }

    [MaxLength(200)]
    [Column("title")]
    public string? Title { get; set; }

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Required]
    [Column("primary_mood_id")]
    public int PrimaryMoodId { get; set; }

    [Column("word_count")]
    public int WordCount { get; set; }

    [Column("is_favorite")]
    public bool IsFavorite { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("PrimaryMoodId")]
    public virtual Mood PrimaryMood { get; set; } = null!;

    public virtual ICollection<EntryTag> EntryTags { get; set; } = new List<EntryTag>();
    public virtual ICollection<EntrySecondaryMood> SecondaryMoods { get; set; } = new List<EntrySecondaryMood>();
}
