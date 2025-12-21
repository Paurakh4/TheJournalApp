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
