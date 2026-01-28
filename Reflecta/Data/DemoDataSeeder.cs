using Microsoft.EntityFrameworkCore;
using Reflecta.Models;

namespace Reflecta.Data;

public static class DemoDataSeeder
{
    private const string DemoUserEmail = "user@demo.com";

    public static async Task SeedDemoEntriesAsync(ReflectaDbContext context)
    {
        // Find the demo user
        var demoUser = await context.Users.FirstOrDefaultAsync(u => u.Email == DemoUserEmail);
        if (demoUser == null)
        {
            return;
        }

        // Check if demo entries already exist (if user has 5+ entries, skip seeding)
        var existingEntryCount = await context.JournalEntries.CountAsync(e => e.UserId == demoUser.Id);
        if (existingEntryCount >= 5)
        {
            return;
        }

        // Get system tags for use in entries
        var systemTags = await context.Tags.Where(t => t.IsSystem).ToListAsync();
        var workTag = systemTags.FirstOrDefault(t => t.Name == "Work");
        var personalTag = systemTags.FirstOrDefault(t => t.Name == "Personal");
        var healthTag = systemTags.FirstOrDefault(t => t.Name == "Health");
        var familyTag = systemTags.FirstOrDefault(t => t.Name == "Family");
        var goalsTag = systemTags.FirstOrDefault(t => t.Name == "Goals");
        var learningTag = systemTags.FirstOrDefault(t => t.Name == "Learning");
        var mindfulnessTag = systemTags.FirstOrDefault(t => t.Name == "Mindfulness");
        var creativityTag = systemTags.FirstOrDefault(t => t.Name == "Creativity");
        var travelTag = systemTags.FirstOrDefault(t => t.Name == "Travel");
        var relationshipsTag = systemTags.FirstOrDefault(t => t.Name == "Relationships");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Create 12 demo journal entries spanning the last 14 days
        var demoEntries = new List<(JournalEntry Entry, int[] SecondaryMoodIds, Guid[] TagIds)>
        {
            // Entry 1 - 14 days ago (Excited about new project)
            (new JournalEntry
            {
                UserId = demoUser.Id,
                EntryDate = today.AddDays(-14),
                Title = "Starting a new chapter",
                Content = "Today marks the beginning of something exciting! I've been assigned to lead the new project at work. It's a bit overwhelming, but I feel ready for this challenge. The team seems enthusiastic, and I can't wait to see where this journey takes us. I spent the evening planning out the first sprint and brainstorming ideas.",
                PrimaryMoodId = 2, // Excited
                WordCount = 64,
                IsFavorite = true,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14)
            }, new[] { 6 }, new[] { workTag?.Id ?? Guid.Empty, goalsTag?.Id ?? Guid.Empty }), // Secondary: Proud

            // Entry 2 - 13 days ago (Calm morning routine)
            (new JournalEntry
            {
                UserId = demoUser.Id,
                EntryDate = today.AddDays(-13),
                Title = "Morning meditation breakthrough",
                Content = "Woke up early today and finally managed to complete a full 20-minute meditation session without my mind wandering too much. It's been weeks of practice, but I think I'm finally getting the hang of it. The rest of the day felt more centered and peaceful. Maybe there's something to this mindfulness thing after all.",
                PrimaryMoodId = 4, // Calm
                WordCount = 62,
                IsFavorite = false,
                CreatedAt = DateTime.UtcNow.AddDays(-13),
                UpdatedAt = DateTime.UtcNow.AddDays(-13)
            }, new[] { 3 }, new[] { healthTag?.Id ?? Guid.Empty, mindfulnessTag?.Id ?? Guid.Empty }), // Secondary: Grateful

            // Entry 3 - 12 days ago (Stressed about deadlines)
            (new JournalEntry
            {
                UserId = demoUser.Id,
                EntryDate = today.AddDays(-12),
                Title = "Deadline pressure",
                Content = "The project timeline just got moved up by two weeks. I'm feeling the pressure now. Had to work late and skip my evening workout. I know this isn't sustainable, but sometimes you just have to push through. Hoping things settle down soon. Made a list of priorities to tackle tomorrow.",
                PrimaryMoodId = 14, // Stressed
                WordCount = 58,
                IsFavorite = false,
                CreatedAt = DateTime.UtcNow.AddDays(-12),
                UpdatedAt = DateTime.UtcNow.AddDays(-12)
            }, new[] { 12 }, new[] { workTag?.Id ?? Guid.Empty }), // Secondary: Anxious

            // Entry 4 - 10 days ago (Happy family dinner)
            (new JournalEntry
            {
                UserId = demoUser.Id,
                EntryDate = today.AddDays(-10),
                Title = "Sunday dinner with family",
                Content = "Had the whole family over for dinner today. Mom made her famous lasagna, and we spent hours just talking and laughing. It's moments like these that remind me what's really important in life. My nephew showed me his new drawings - he's getting really talented! These gatherings always recharge my soul.",
                PrimaryMoodId = 1, // Happy
                WordCount = 60,
                IsFavorite = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            }, new[] { 5, 3 }, new[] { familyTag?.Id ?? Guid.Empty, relationshipsTag?.Id ?? Guid.Empty }), // Secondary: Loved, Grateful

            // Entry 5 - 9 days ago (Thinking about career)
            (new JournalEntry
            {
                UserId = demoUser.Id,
                EntryDate = today.AddDays(-9),
                Title = "Career reflections",
                Content = "Been thinking a lot about where I want to be in five years. The current role is great, but I wonder if I should be pursuing more leadership opportunities. Started researching some online courses for project management certification. It's never too late to invest in yourself, right?",
                PrimaryMoodId = 9, // Thinking
                WordCount = 54,
                IsFavorite = false,
                CreatedAt = DateTime.UtcNow.AddDays(-9),
                UpdatedAt = DateTime.UtcNow.AddDays(-9)
            }, new[] { 7 }, new[] { learningTag?.Id ?? Guid.Empty, goalsTag?.Id ?? Guid.Empty }), // Secondary: Okay

            // Entry 6 - 7 days ago (Proud of achievement)
            (new JournalEntry
            {
                UserId = demoUser.Id,
                EntryDate = today.AddDays(-7),
                Title = "First milestone achieved!",
                Content = "We hit our first major milestone on the project today! The demo went smoothly, and the stakeholders were impressed. All those late nights paid off. Treated the team to lunch to celebrate. It feels good to see hard work recognized. Now onto the next phase!",
                PrimaryMoodId = 6, // Proud
                WordCount = 52,
                IsFavorite = true,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-7)
            }, new[] { 1, 2 }, new[] { workTag?.Id ?? Guid.Empty, goalsTag?.Id ?? Guid.Empty }), // Secondary: Happy, Excited

            // Entry 7 - 6 days ago (Tired but content)
            (new JournalEntry
            {
                UserId = demoUser.Id,
                EntryDate = today.AddDays(-6),
                Title = "Rest day",
                Content = "Finally took a proper rest day. Slept in, watched some movies, and didn't look at my work email once. I needed this. Sometimes you have to give yourself permission to do nothing. Tomorrow I'll be back at it, but today was about recharging. Made some homemade soup for dinner - comfort food at its finest.",
                PrimaryMoodId = 8, // Tired
                WordCount = 59,
                IsFavorite = false,
                CreatedAt = DateTime.UtcNow.AddDays(-6),
                UpdatedAt = DateTime.UtcNow.AddDays(-6)
            }, new[] { 4 }, new[] { personalTag?.Id ?? Guid.Empty, healthTag?.Id ?? Guid.Empty }), // Secondary: Calm

            // Entry 8 - 5 days ago (Anxious about presentation)
            (new JournalEntry
            {
                UserId = demoUser.Id,
                EntryDate = today.AddDays(-5),
                Title = "Presentation prep",
                Content = "Big presentation to the executives tomorrow. I've practiced it a dozen times, but I still feel nervous. What if they don't like our approach? What if I forget something important? Taking deep breaths and reminding myself that I know this material inside out. Going to get an early night.",
                PrimaryMoodId = 12, // Anxious
                WordCount = 55,
                IsFavorite = false,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            }, new[] { 9 }, new[] { workTag?.Id ?? Guid.Empty }), // Secondary: Thinking

            // Entry 9 - 4 days ago (Grateful for support)
            (new JournalEntry
            {
                UserId = demoUser.Id,
                EntryDate = today.AddDays(-4),
                Title = "Presentation success!",
                Content = "The presentation went better than I could have hoped! The executives approved our budget increase and timeline. My manager pulled me aside afterward and said she was proud of my growth. Feeling so grateful for the supportive team and mentors I have. Celebrated with my favorite coffee and a walk in the park.",
                PrimaryMoodId = 3, // Grateful
                WordCount = 58,
                IsFavorite = true,
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                UpdatedAt = DateTime.UtcNow.AddDays(-4)
            }, new[] { 1, 6 }, new[] { workTag?.Id ?? Guid.Empty, relationshipsTag?.Id ?? Guid.Empty }), // Secondary: Happy, Proud

            // Entry 10 - 3 days ago (Creative outlet)
            (new JournalEntry
            {
                UserId = demoUser.Id,
                EntryDate = today.AddDays(-3),
                Title = "Rediscovering hobbies",
                Content = "Picked up my guitar for the first time in months. My fingers are rusty, but it felt so good to play again. Music has always been my escape. I'm going to make time for this every week, no excuses. Also sketched a bit while listening to some jazz. Creativity feeds the soul.",
                PrimaryMoodId = 4, // Calm
                WordCount = 55,
                IsFavorite = false,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            }, new[] { 1 }, new[] { creativityTag?.Id ?? Guid.Empty, personalTag?.Id ?? Guid.Empty }), // Secondary: Happy

            // Entry 11 - 2 days ago (Planning a trip)
            (new JournalEntry
            {
                UserId = demoUser.Id,
                EntryDate = today.AddDays(-2),
                Title = "Vacation planning",
                Content = "Started planning a trip for next month. Looking at beach destinations - I desperately need some sun and sand. Found a great deal on a resort that has excellent reviews. The anticipation of travel is almost as good as the trip itself. Already mentally packing my bags!",
                PrimaryMoodId = 2, // Excited
                WordCount = 52,
                IsFavorite = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            }, new[] { 1 }, new[] { travelTag?.Id ?? Guid.Empty, personalTag?.Id ?? Guid.Empty }), // Secondary: Happy

            // Entry 12 - Yesterday (Reflection on the week)
            (new JournalEntry
            {
                UserId = demoUser.Id,
                EntryDate = today.AddDays(-1),
                Title = "Week in review",
                Content = "What a week it's been! Looking back, I've accomplished more than I realized. Hit project milestones, reconnected with family, made time for myself, and even started planning a vacation. Life isn't perfect, but it's pretty good right now. Grateful for the highs and learning from the lows. Here's to another week of growth.",
                PrimaryMoodId = 3, // Grateful
                WordCount = 61,
                IsFavorite = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            }, new[] { 1, 4 }, new[] { personalTag?.Id ?? Guid.Empty, mindfulnessTag?.Id ?? Guid.Empty }) // Secondary: Happy, Calm
        };

        // Add entries to database
        foreach (var (entry, secondaryMoodIds, tagIds) in demoEntries)
        {
            // Check if entry for this date already exists
            var existingEntry = await context.JournalEntries
                .FirstOrDefaultAsync(e => e.UserId == demoUser.Id && e.EntryDate == entry.EntryDate);
            
            if (existingEntry != null)
            {
                continue; // Skip if entry already exists for this date
            }

            context.JournalEntries.Add(entry);
            await context.SaveChangesAsync();

            // Add secondary moods (max 2)
            foreach (var moodId in secondaryMoodIds.Distinct().Take(2))
            {
                if (moodId != entry.PrimaryMoodId)
                {
                    context.EntrySecondaryMoods.Add(new EntrySecondaryMood
                    {
                        EntryId = entry.Id,
                        MoodId = moodId,
                        CreatedAt = entry.CreatedAt
                    });
                }
            }

            // Add tags
            foreach (var tagId in tagIds.Where(t => t != Guid.Empty).Distinct())
            {
                context.EntryTags.Add(new EntryTag
                {
                    EntryId = entry.Id,
                    TagId = tagId,
                    CreatedAt = entry.CreatedAt
                });
            }

            await context.SaveChangesAsync();
        }

        // Update streak for demo user
        var streak = await context.Streaks.FirstOrDefaultAsync(s => s.UserId == demoUser.Id);
        if (streak != null)
        {
            streak.CurrentStreak = 2; // Has entries for yesterday and 2 days ago
            streak.LongestStreak = 3;
            streak.LastEntryDate = today.AddDays(-1);
            streak.StreakStartDate = today.AddDays(-2);
            streak.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }
}
