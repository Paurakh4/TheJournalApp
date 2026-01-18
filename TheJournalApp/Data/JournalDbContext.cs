using Microsoft.EntityFrameworkCore;
using TheJournalApp.Data.Entities;

namespace TheJournalApp.Data;

public class JournalDbContext : DbContext
{
    public JournalDbContext(DbContextOptions<JournalDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<Streak> Streaks => Set<Streak>();
    public DbSet<Mood> Moods => Set<Mood>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<EntrySecondaryMood> EntrySecondaryMoods => Set<EntrySecondaryMood>();
    public DbSet<EntryTag> EntryTags => Set<EntryTag>();
    public DbSet<ExportLog> ExportLogs => Set<ExportLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // UserSettings configuration (1:1 with User)
        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.HasKey(e => e.SettingId);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.IsDarkMode).HasDefaultValue(false);
            entity.Property(e => e.AppPin).HasMaxLength(255);
            entity.Property(e => e.RequirePinOnLaunch).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                .WithOne(u => u.Settings)
                .HasForeignKey<UserSettings>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Streak configuration (1:1 with User)
        modelBuilder.Entity<Streak>(entity =>
        {
            entity.HasKey(e => e.StreakId);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.CurrentStreak).HasDefaultValue(0);
            entity.Property(e => e.LongestStreak).HasDefaultValue(0);
            
            entity.HasOne(e => e.User)
                .WithOne(u => u.Streak)
                .HasForeignKey<Streak>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Mood configuration
        modelBuilder.Entity<Mood>(entity =>
        {
            entity.HasKey(e => e.MoodId);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Category).IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(e => e.Emoji).IsRequired().HasMaxLength(10);
            entity.Property(e => e.IsSystem).HasDefaultValue(true);
        });

        // Tag configuration
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.TagId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsBuiltin).HasDefaultValue(false);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Tags)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // JournalEntry configuration
        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.HasKey(e => e.EntryId);
            entity.HasIndex(e => new { e.UserId, e.EntryDate }).IsUnique();
            entity.HasIndex(e => e.PrimaryMoodId);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.WordCount).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.JournalEntries)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.PrimaryMood)
                .WithMany(m => m.PrimaryEntries)
                .HasForeignKey(e => e.PrimaryMoodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // EntrySecondaryMood configuration (junction table)
        modelBuilder.Entity<EntrySecondaryMood>(entity =>
        {
            entity.HasKey(e => new { e.EntryId, e.MoodId });
            
            entity.HasOne(e => e.Entry)
                .WithMany(je => je.SecondaryMoods)
                .HasForeignKey(e => e.EntryId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Mood)
                .WithMany(m => m.SecondaryMoodEntries)
                .HasForeignKey(e => e.MoodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // EntryTag configuration (junction table)
        modelBuilder.Entity<EntryTag>(entity =>
        {
            entity.HasKey(e => new { e.EntryId, e.TagId });
            entity.HasIndex(e => e.TagId);
            
            entity.HasOne(e => e.Entry)
                .WithMany(je => je.EntryTags)
                .HasForeignKey(e => e.EntryId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Tag)
                .WithMany(t => t.EntryTags)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ExportLog configuration
        modelBuilder.Entity<ExportLog>(entity =>
        {
            entity.HasKey(e => e.ExportId);
            entity.Property(e => e.Format).HasDefaultValue("PDF").HasMaxLength(50);
            entity.Property(e => e.ExportedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.ExportLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed default moods
        SeedMoods(modelBuilder);
        
        // Seed default tags
        SeedTags(modelBuilder);
    }

    private static void SeedMoods(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Mood>().HasData(
            new Mood { MoodId = 1, Name = "Happy", Category = MoodCategory.Positive, Emoji = "😊", IsSystem = true },
            new Mood { MoodId = 2, Name = "Excited", Category = MoodCategory.Positive, Emoji = "🤩", IsSystem = true },
            new Mood { MoodId = 3, Name = "Grateful", Category = MoodCategory.Positive, Emoji = "🙏", IsSystem = true },
            new Mood { MoodId = 4, Name = "Calm", Category = MoodCategory.Positive, Emoji = "😌", IsSystem = true },
            new Mood { MoodId = 5, Name = "Loved", Category = MoodCategory.Positive, Emoji = "🥰", IsSystem = true },
            new Mood { MoodId = 6, Name = "Neutral", Category = MoodCategory.Neutral, Emoji = "😐", IsSystem = true },
            new Mood { MoodId = 7, Name = "Tired", Category = MoodCategory.Neutral, Emoji = "😴", IsSystem = true },
            new Mood { MoodId = 8, Name = "Bored", Category = MoodCategory.Neutral, Emoji = "😑", IsSystem = true },
            new Mood { MoodId = 9, Name = "Sad", Category = MoodCategory.Negative, Emoji = "😢", IsSystem = true },
            new Mood { MoodId = 10, Name = "Anxious", Category = MoodCategory.Negative, Emoji = "😰", IsSystem = true },
            new Mood { MoodId = 11, Name = "Angry", Category = MoodCategory.Negative, Emoji = "😠", IsSystem = true },
            new Mood { MoodId = 12, Name = "Stressed", Category = MoodCategory.Negative, Emoji = "😫", IsSystem = true },
            new Mood { MoodId = 13, Name = "Lonely", Category = MoodCategory.Negative, Emoji = "😔", IsSystem = true }
        );
    }

    private static void SeedTags(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tag>().HasData(
            new Tag { TagId = 1, UserId = null, Name = "Work", IsBuiltin = true },
            new Tag { TagId = 2, UserId = null, Name = "Personal", IsBuiltin = true },
            new Tag { TagId = 3, UserId = null, Name = "Health", IsBuiltin = true },
            new Tag { TagId = 4, UserId = null, Name = "Family", IsBuiltin = true },
            new Tag { TagId = 5, UserId = null, Name = "Travel", IsBuiltin = true },
            new Tag { TagId = 6, UserId = null, Name = "Exercise", IsBuiltin = true },
            new Tag { TagId = 7, UserId = null, Name = "Food", IsBuiltin = true },
            new Tag { TagId = 8, UserId = null, Name = "Social", IsBuiltin = true },
            new Tag { TagId = 9, UserId = null, Name = "Hobby", IsBuiltin = true },
            new Tag { TagId = 10, UserId = null, Name = "Learning", IsBuiltin = true }
        );
    }
}
