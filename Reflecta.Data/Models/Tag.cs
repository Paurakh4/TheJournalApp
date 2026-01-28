using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reflecta.Data.Models;

[Table("tags")]
public class Tag
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(7)]
    [Column("color")]
    public string? Color { get; set; } // Hex color code e.g., "#FF5733"

    [Column("is_system")]
    public bool IsSystem { get; set; } = false; // System-defined vs user-created

    [Column("user_id")]
    public Guid? UserId { get; set; } // Null for system tags, set for user-created tags

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
    
    public virtual ICollection<EntryTag> EntryTags { get; set; } = new List<EntryTag>();
}
