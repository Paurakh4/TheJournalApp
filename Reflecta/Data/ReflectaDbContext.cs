using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Reflecta.Models;

namespace Reflecta.Data;

public class ReflectaDbContext : DbContext
{
    public ReflectaDbContext(DbContextOptions<ReflectaDbContext> options) : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<Mood> Moods => Set<Mood>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<EntryTag> EntryTags => Set<EntryTag>();
    public DbSet<EntrySecondaryMood> EntrySecondaryMoods => Set<EntrySecondaryMood>();
    public DbSet<Streak> Streaks => Set<Streak>();
    public DbSet<ExportLog> ExportLogs => Set<ExportLog>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        // Suppress the PendingModelChangesWarning for migrations
        optionsBuilder.ConfigureWarnings(w => 
            w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasOne(u => u.Settings)
                .WithOne(s => s.User)
                .HasForeignKey<UserSettings>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(u => u.Streak)
                .WithOne(s => s.User)
                .HasForeignKey<Streak>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // JournalEntry configuration
        modelBuilder.Entity<JournalEntry>(entity =>
        {
            // Enforce one entry per user per day
            entity.HasIndex(e => new { e.UserId, e.EntryDate }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.JournalEntries)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.PrimaryMood)
                .WithMany(m => m.PrimaryMoodEntries)
                .HasForeignKey(e => e.PrimaryMoodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // EntryTag junction table (Many-to-Many: Entry <-> Tag)
        modelBuilder.Entity<EntryTag>(entity =>
        {
            entity.HasKey(et => new { et.EntryId, et.TagId });

            entity.HasOne(et => et.Entry)
                .WithMany(e => e.EntryTags)
                .HasForeignKey(et => et.EntryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(et => et.Tag)
                .WithMany(t => t.EntryTags)
                .HasForeignKey(et => et.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // EntrySecondaryMood junction table (Many-to-Many: Entry <-> Mood for secondary moods)
        modelBuilder.Entity<EntrySecondaryMood>(entity =>
        {
            entity.HasKey(esm => new { esm.EntryId, esm.MoodId });

            entity.HasOne(esm => esm.Entry)
                .WithMany(e => e.SecondaryMoods)
                .HasForeignKey(esm => esm.EntryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(esm => esm.Mood)
                .WithMany(m => m.SecondaryMoodEntries)
                .HasForeignKey(esm => esm.MoodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Tag configuration
        modelBuilder.Entity<Tag>(entity =>
        {
            // Unique tag name per user (or system)
            entity.HasIndex(t => new { t.Name, t.UserId }).IsUnique();

            entity.HasOne(t => t.User)
                .WithMany(u => u.CustomTags)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
        });

        // Mood configuration - store enum as string
        modelBuilder.Entity<Mood>(entity =>
        {
            entity.Property(m => m.Category)
                .HasConversion<string>()
                .HasMaxLength(20);
        });

        // ExportLog configuration - store enum as string
        modelBuilder.Entity<ExportLog>(entity =>
        {
            entity.Property(e => e.ExportFormat)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasOne(e => e.User)
                .WithMany(u => u.ExportLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed predefined moods
        SeedMoods(modelBuilder);

        // Seed predefined system tags
        SeedTags(modelBuilder);
    }

    private static void SeedMoods(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Mood>().HasData(
            // Positive moods
            new Mood { Id = 1, Name = "Happy", Emoji = "😊", Category = MoodCategory.Positive, DisplayOrder = 1 },
            new Mood { Id = 2, Name = "Excited", Emoji = "🤩", Category = MoodCategory.Positive, DisplayOrder = 2 },
            new Mood { Id = 3, Name = "Grateful", Emoji = "🙏", Category = MoodCategory.Positive, DisplayOrder = 3 },
            new Mood { Id = 4, Name = "Calm", Emoji = "😌", Category = MoodCategory.Positive, DisplayOrder = 4 },
            new Mood { Id = 5, Name = "Loved", Emoji = "🥰", Category = MoodCategory.Positive, DisplayOrder = 5 },
            new Mood { Id = 6, Name = "Proud", Emoji = "💪", Category = MoodCategory.Positive, DisplayOrder = 6 },

            // Neutral moods
            new Mood { Id = 7, Name = "Okay", Emoji = "😐", Category = MoodCategory.Neutral, DisplayOrder = 7 },
            new Mood { Id = 8, Name = "Tired", Emoji = "😴", Category = MoodCategory.Neutral, DisplayOrder = 8 },
            new Mood { Id = 9, Name = "Thinking", Emoji = "🤔", Category = MoodCategory.Neutral, DisplayOrder = 9 },
            new Mood { Id = 10, Name = "Bored", Emoji = "😑", Category = MoodCategory.Neutral, DisplayOrder = 10 },

            // Negative moods
            new Mood { Id = 11, Name = "Sad", Emoji = "😢", Category = MoodCategory.Negative, DisplayOrder = 11 },
            new Mood { Id = 12, Name = "Anxious", Emoji = "😰", Category = MoodCategory.Negative, DisplayOrder = 12 },
            new Mood { Id = 13, Name = "Angry", Emoji = "😠", Category = MoodCategory.Negative, DisplayOrder = 13 },
            new Mood { Id = 14, Name = "Stressed", Emoji = "😫", Category = MoodCategory.Negative, DisplayOrder = 14 },
            new Mood { Id = 15, Name = "Lonely", Emoji = "😔", Category = MoodCategory.Negative, DisplayOrder = 15 }
        );
    }

    private static void SeedTags(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tag>().HasData(
            new Tag { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Work", Color = "#4A90D9", IsSystem = true, UserId = null, Category = "Work" },
            new Tag { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Personal", Color = "#7ED321", IsSystem = true, UserId = null, Category = "Personal" },
            new Tag { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Health", Color = "#F5A623", IsSystem = true, UserId = null, Category = "Health" },
            new Tag { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Family", Color = "#D0021B", IsSystem = true, UserId = null, Category = "Relationships" },
            new Tag { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Travel", Color = "#9013FE", IsSystem = true, UserId = null, Category = "Travel" },
            new Tag { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Goals", Color = "#50E3C2", IsSystem = true, UserId = null, Category = "Personal" },
            new Tag { Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), Name = "Learning", Color = "#BD10E0", IsSystem = true, UserId = null, Category = "Lifestyle" },
            new Tag { Id = Guid.Parse("88888888-8888-8888-8888-888888888888"), Name = "Relationships", Color = "#FF6B6B", IsSystem = true, UserId = null, Category = "Relationships" },
            new Tag { Id = Guid.Parse("99999999-9999-9999-9999-999999999999"), Name = "Mindfulness", Color = "#4ECDC4", IsSystem = true, UserId = null, Category = "Health" },
            new Tag { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Name = "Creativity", Color = "#FFE66D", IsSystem = true, UserId = null, Category = "Lifestyle" }
        );
    }
}
