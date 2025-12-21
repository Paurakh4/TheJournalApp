namespace TheJournalApp.Data.Entities;

public class Streak
{
    public int StreakId { get; set; }
    public int UserId { get; set; }
    public int CurrentStreak { get; set; } = 0;
    public int LongestStreak { get; set; } = 0;
    public DateOnly? LastEntryDate { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}
