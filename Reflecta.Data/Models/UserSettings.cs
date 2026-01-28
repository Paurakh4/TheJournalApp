using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reflecta.Data.Models;

[Table("user_settings")]
public class UserSettings
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("theme")]
    public string Theme { get; set; } = "dark"; // "light" or "dark"

    [MaxLength(255)]
    [Column("pin_hash")]
    public string? PinHash { get; set; } // Optional app-level PIN

    [Column("pin_enabled")]
    public bool PinEnabled { get; set; } = false;

    [Column("reminder_enabled")]
    public bool ReminderEnabled { get; set; } = false;

    [Column("reminder_time")]
    public TimeOnly? ReminderTime { get; set; }

    [MaxLength(100)]
    [Column("time_zone_id")]
    public string? TimeZoneId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
