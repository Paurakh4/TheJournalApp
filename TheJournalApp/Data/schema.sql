-- The Journal App Database Schema
-- PostgreSQL

-- Create ENUM type for MoodCategory
DO $$ BEGIN
    CREATE TYPE mood_category AS ENUM ('Positive', 'Neutral', 'Negative');
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

-- Users table
CREATE TABLE IF NOT EXISTS "Users" (
    "UserId" SERIAL PRIMARY KEY,
    "Username" VARCHAR(255) NOT NULL UNIQUE,
    "PasswordHash" VARCHAR(255) NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- UserSettings table (1:1 with Users)
CREATE TABLE IF NOT EXISTS "UserSettings" (
    "SettingId" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL UNIQUE,
    "IsDarkMode" BOOLEAN DEFAULT FALSE,
    "AppPin" VARCHAR(255),
    "RequirePinOnLaunch" BOOLEAN DEFAULT FALSE,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserSettings_Users" FOREIGN KEY ("UserId") REFERENCES "Users" ("UserId") ON DELETE CASCADE
);

-- Streaks table (1:1 with Users)
CREATE TABLE IF NOT EXISTS "Streaks" (
    "StreakId" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL UNIQUE,
    "CurrentStreak" INTEGER DEFAULT 0,
    "LongestStreak" INTEGER DEFAULT 0,
    "LastEntryDate" DATE,
    CONSTRAINT "FK_Streaks_Users" FOREIGN KEY ("UserId") REFERENCES "Users" ("UserId") ON DELETE CASCADE
);

-- Moods table
CREATE TABLE IF NOT EXISTS "Moods" (
    "MoodId" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL UNIQUE,
    "Category" VARCHAR(20) NOT NULL,
    "Emoji" VARCHAR(10) NOT NULL,
    "IsSystem" BOOLEAN DEFAULT TRUE
);

-- Tags table
CREATE TABLE IF NOT EXISTS "Tags" (
    "TagId" SERIAL PRIMARY KEY,
    "UserId" INTEGER,
    "Name" VARCHAR(100) NOT NULL,
    "IsBuiltin" BOOLEAN DEFAULT FALSE,
    CONSTRAINT "FK_Tags_Users" FOREIGN KEY ("UserId") REFERENCES "Users" ("UserId") ON DELETE CASCADE
);

-- JournalEntries table
CREATE TABLE IF NOT EXISTS "JournalEntries" (
    "EntryId" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "PrimaryMoodId" INTEGER NOT NULL,
    "Content" TEXT NOT NULL,
    "WordCount" INTEGER DEFAULT 0,
    "EntryDate" DATE NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_JournalEntries_Users" FOREIGN KEY ("UserId") REFERENCES "Users" ("UserId") ON DELETE CASCADE,
    CONSTRAINT "FK_JournalEntries_Moods" FOREIGN KEY ("PrimaryMoodId") REFERENCES "Moods" ("MoodId") ON DELETE RESTRICT,
    CONSTRAINT "UQ_JournalEntries_UserDate" UNIQUE ("UserId", "EntryDate")
);

-- EntrySecondaryMoods junction table
CREATE TABLE IF NOT EXISTS "EntrySecondaryMoods" (
    "EntryId" INTEGER NOT NULL,
    "MoodId" INTEGER NOT NULL,
    PRIMARY KEY ("EntryId", "MoodId"),
    CONSTRAINT "FK_EntrySecondaryMoods_Entries" FOREIGN KEY ("EntryId") REFERENCES "JournalEntries" ("EntryId") ON DELETE CASCADE,
    CONSTRAINT "FK_EntrySecondaryMoods_Moods" FOREIGN KEY ("MoodId") REFERENCES "Moods" ("MoodId") ON DELETE RESTRICT
);

-- EntryTags junction table
CREATE TABLE IF NOT EXISTS "EntryTags" (
    "EntryId" INTEGER NOT NULL,
    "TagId" INTEGER NOT NULL,
    PRIMARY KEY ("EntryId", "TagId"),
    CONSTRAINT "FK_EntryTags_Entries" FOREIGN KEY ("EntryId") REFERENCES "JournalEntries" ("EntryId") ON DELETE CASCADE,
    CONSTRAINT "FK_EntryTags_Tags" FOREIGN KEY ("TagId") REFERENCES "Tags" ("TagId") ON DELETE RESTRICT
);

-- ExportLogs table
CREATE TABLE IF NOT EXISTS "ExportLogs" (
    "ExportId" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "RangeStart" DATE NOT NULL,
    "RangeEnd" DATE NOT NULL,
    "Format" VARCHAR(50) DEFAULT 'PDF',
    "ExportedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_ExportLogs_Users" FOREIGN KEY ("UserId") REFERENCES "Users" ("UserId") ON DELETE CASCADE
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS "IX_JournalEntries_UserId_EntryDate" ON "JournalEntries" ("UserId", "EntryDate" DESC);
CREATE INDEX IF NOT EXISTS "IX_JournalEntries_PrimaryMoodId" ON "JournalEntries" ("PrimaryMoodId");
CREATE INDEX IF NOT EXISTS "IX_EntryTags_TagId" ON "EntryTags" ("TagId");
CREATE INDEX IF NOT EXISTS "IX_Tags_UserId" ON "Tags" ("UserId");

-- Seed default moods
INSERT INTO "Moods" ("MoodId", "Name", "Category", "Emoji", "IsSystem") VALUES
    (1, 'Happy', 'Positive', '😊', TRUE),
    (2, 'Excited', 'Positive', '🤩', TRUE),
    (3, 'Grateful', 'Positive', '🙏', TRUE),
    (4, 'Calm', 'Positive', '😌', TRUE),
    (5, 'Loved', 'Positive', '🥰', TRUE),
    (6, 'Neutral', 'Neutral', '😐', TRUE),
    (7, 'Tired', 'Neutral', '😴', TRUE),
    (8, 'Bored', 'Neutral', '😑', TRUE),
    (9, 'Sad', 'Negative', '😢', TRUE),
    (10, 'Anxious', 'Negative', '😰', TRUE),
    (11, 'Angry', 'Negative', '😠', TRUE),
    (12, 'Stressed', 'Negative', '😫', TRUE),
    (13, 'Lonely', 'Negative', '😔', TRUE)
ON CONFLICT ("MoodId") DO NOTHING;

-- Seed default tags (system tags have NULL UserId)
INSERT INTO "Tags" ("TagId", "UserId", "Name", "IsBuiltin") VALUES
    (1, NULL, 'Work', TRUE),
    (2, NULL, 'Personal', TRUE),
    (3, NULL, 'Health', TRUE),
    (4, NULL, 'Family', TRUE),
    (5, NULL, 'Travel', TRUE),
    (6, NULL, 'Exercise', TRUE),
    (7, NULL, 'Food', TRUE),
    (8, NULL, 'Social', TRUE),
    (9, NULL, 'Hobby', TRUE),
    (10, NULL, 'Learning', TRUE)
ON CONFLICT ("TagId") DO NOTHING;

-- Reset sequences to avoid conflicts
SELECT setval('"Moods_MoodId_seq"', (SELECT MAX("MoodId") FROM "Moods"));
SELECT setval('"Tags_TagId_seq"', (SELECT MAX("TagId") FROM "Tags"));

-- Seed demo user
INSERT INTO "Users" ("UserId", "Username", "PasswordHash", "CreatedAt") VALUES
    (1, 'User', 'demo', CURRENT_TIMESTAMP)
ON CONFLICT ("UserId") DO NOTHING;

-- Seed user settings for demo user
INSERT INTO "UserSettings" ("UserId", "IsDarkMode") VALUES
    (1, FALSE)
ON CONFLICT ("UserId") DO NOTHING;

-- Seed streak for demo user (12 consecutive days)
INSERT INTO "Streaks" ("UserId", "CurrentStreak", "LongestStreak", "LastEntryDate") VALUES
    (1, 12, 12, CURRENT_DATE)
ON CONFLICT ("UserId") DO NOTHING;

-- Seed demo journal entries (12 consecutive days)
INSERT INTO "JournalEntries" ("EntryId", "UserId", "PrimaryMoodId", "Content", "WordCount", "EntryDate", "CreatedAt", "UpdatedAt") VALUES
    (1, 1, 1, 'Had an amazing day at work today! Finally finished the project I''ve been working on for weeks. The team was super supportive and we celebrated with lunch together. Feeling accomplished and ready for new challenges.', 36, CURRENT_DATE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    (2, 1, 3, 'Grateful for the little things today. Woke up to sunshine, had my favorite coffee, and received a sweet message from an old friend. Sometimes it''s the small moments that matter most.', 33, CURRENT_DATE - INTERVAL '1 day', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    (3, 1, 2, 'So excited about the upcoming vacation! Started packing and planning the itinerary. Can''t wait to explore new places and create memories with family.', 26, CURRENT_DATE - INTERVAL '2 days', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    (4, 1, 7, 'Long day at work. Had back-to-back meetings and didn''t get much done on my actual tasks. Need to find a better balance. Going to bed early tonight to recharge.', 31, CURRENT_DATE - INTERVAL '3 days', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    (5, 1, 4, 'Peaceful morning meditation followed by a quiet walk in the park. The autumn leaves are beautiful this time of year. Feeling centered and at peace with everything.', 28, CURRENT_DATE - INTERVAL '4 days', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    (6, 1, 10, 'Feeling a bit anxious about the presentation tomorrow. Practiced several times but still nervous. Need to remember to breathe and trust my preparation.', 26, CURRENT_DATE - INTERVAL '5 days', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    (7, 1, 1, 'The presentation went great! All that worrying for nothing. Got positive feedback from the team and even a compliment from my manager. Celebrating with pizza tonight!', 28, CURRENT_DATE - INTERVAL '6 days', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    (8, 1, 5, 'Wonderful family dinner at mom''s place. She made her famous pasta and we all sat around sharing stories. These moments are precious. Feeling so loved.', 27, CURRENT_DATE - INTERVAL '7 days', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    (9, 1, 6, 'Regular day, nothing too exciting. Worked from home, did some chores, watched a movie in the evening. Sometimes ordinary days are just what you need.', 26, CURRENT_DATE - INTERVAL '8 days', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    (10, 1, 12, 'Stressed about deadlines piling up. Too many things on my plate right now. Made a to-do list to organize everything. Hope tomorrow is more productive.', 27, CURRENT_DATE - INTERVAL '9 days', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    (11, 1, 2, 'Started learning a new programming language today! It''s challenging but exciting. Love the feeling of expanding my skills. Small progress is still progress.', 26, CURRENT_DATE - INTERVAL '10 days', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    (12, 1, 4, 'Great gym session this morning. Hit a new personal record on squats! Feeling strong and motivated to keep up the routine. Health is wealth.', 25, CURRENT_DATE - INTERVAL '11 days', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT ("EntryId") DO NOTHING;

-- Seed demo entry tags
INSERT INTO "EntryTags" ("EntryId", "TagId") VALUES
    (1, 1), (1, 8),      -- Entry 1: Work, Social
    (2, 2),              -- Entry 2: Personal
    (3, 5), (3, 4),      -- Entry 3: Travel, Family
    (4, 1),              -- Entry 4: Work
    (5, 3), (5, 2),      -- Entry 5: Health, Personal
    (6, 1),              -- Entry 6: Work
    (7, 1), (7, 8),      -- Entry 7: Work, Social
    (8, 4), (8, 7),      -- Entry 8: Family, Food
    (9, 2), (9, 9),      -- Entry 9: Personal, Hobby
    (10, 1),             -- Entry 10: Work
    (11, 10), (11, 9),   -- Entry 11: Learning, Hobby
    (12, 6), (12, 3)     -- Entry 12: Exercise, Health
ON CONFLICT DO NOTHING;

-- Reset sequences after demo data
SELECT setval('"Users_UserId_seq"', (SELECT COALESCE(MAX("UserId"), 1) FROM "Users"));
SELECT setval('"JournalEntries_EntryId_seq"', (SELECT COALESCE(MAX("EntryId"), 1) FROM "JournalEntries"));
