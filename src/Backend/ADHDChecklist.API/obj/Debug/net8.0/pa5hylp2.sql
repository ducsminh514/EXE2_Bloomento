IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Roles] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);

CREATE TABLE [User] (
    [Id] uniqueidentifier NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [FullName] nvarchar(max) NULL,
    [SubscriptionTier] int NOT NULL,
    [SubscriptionExpiry] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [LastLoginAt] datetime2 NULL,
    [TimeZone] nvarchar(max) NULL,
    [IsActive] bit NULL,
    CONSTRAINT [PK_User] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [FullName] nvarchar(100) NULL,
    [SubscriptionTier] int NOT NULL DEFAULT 0,
    [SubscriptionExpiry] datetime2 NULL,
    [IsEmailVerified] bit NOT NULL DEFAULT CAST(0 AS bit),
    [EmailVerificationToken] nvarchar(100) NULL,
    [EmailVerificationTokenExpiry] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [LastLoginAt] datetime2 NULL,
    [TimeZone] nvarchar(50) NOT NULL DEFAULT N'UTC',
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [GoogleId] nvarchar(100) NULL,
    [GoogleProfilePicture] nvarchar(500) NULL,
    [RefreshToken] nvarchar(500) NULL,
    [RefreshTokenExpiry] datetime2 NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [RoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_RoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AnalyticsSnapshots] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [SnapshotDate] date NOT NULL,
    [TasksCompleted] int NOT NULL DEFAULT 0,
    [TasksCreated] int NOT NULL DEFAULT 0,
    [TotalFocusMinutes] int NOT NULL DEFAULT 0,
    [HabitsCompleted] int NOT NULL DEFAULT 0,
    [HourlyBreakdown] NVARCHAR(MAX) NULL,
    [CategoryBreakdown] NVARCHAR(MAX) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UserId1] uniqueidentifier NULL,
    CONSTRAINT [PK_AnalyticsSnapshots] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AnalyticsSnapshots_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AnalyticsSnapshots_User_UserId1] FOREIGN KEY ([UserId1]) REFERENCES [User] ([Id])
);

CREATE TABLE [Categories] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Name] nvarchar(50) NOT NULL,
    [ColorHex] nvarchar(7) NOT NULL DEFAULT N'#6B7280',
    [Icon] nvarchar(50) NULL,
    [OrderIndex] int NOT NULL DEFAULT 0,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [ApplicationUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Categories_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Categories_Users_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [Habits] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [ColorHex] nvarchar(7) NOT NULL DEFAULT N'#3B82F6',
    [Frequency] nvarchar(20) NOT NULL,
    [DaysOfWeek] nvarchar(50) NULL,
    [CurrentStreak] int NOT NULL DEFAULT 0,
    [LongestStreak] int NOT NULL DEFAULT 0,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [ApplicationUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_Habits] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Habits_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Habits_Users_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [UserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_UserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_UserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserPreferences] (
    [UserId] uniqueidentifier NOT NULL,
    [ThemeId] int NOT NULL DEFAULT 1,
    [CustomPrimaryColor] nvarchar(7) NULL,
    [CustomAccentColor] nvarchar(7) NULL,
    [DefaultTimeBlockDuration] int NOT NULL DEFAULT 30,
    [AllowFlexibleBlocks] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DefaultFocusLevel] int NOT NULL DEFAULT 2,
    [DefaultWhiteNoise] nvarchar(50) NULL DEFAULT N'none',
    [DefaultPomodoroWork] int NOT NULL DEFAULT 25,
    [DefaultPomodoroBreak] int NOT NULL DEFAULT 5,
    [EnableReminders] bit NOT NULL DEFAULT CAST(1 AS bit),
    [ReminderLeadTime] int NOT NULL DEFAULT 10,
    [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [ApplicationUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_UserPreferences] PRIMARY KEY ([UserId]),
    CONSTRAINT [FK_UserPreferences_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserPreferences_Users_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [UserRoles] (
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserTokens] (
    [UserId] uniqueidentifier NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_UserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Tasks] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [CategoryId] uniqueidentifier NULL,
    [ScheduledDate] date NOT NULL,
    [TimeBlockStart] time NULL,
    [TimeBlockEnd] time NULL,
    [Duration] int NULL,
    [IsCompleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CompletedAt] datetime2 NULL,
    [Priority] int NOT NULL DEFAULT 1,
    [IsRecurring] bit NOT NULL DEFAULT CAST(0 AS bit),
    [RecurrencePattern] nvarchar(50) NULL,
    [ParentTaskId] uniqueidentifier NULL,
    [OrderIndex] int NOT NULL DEFAULT 0,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [DeletedAt] datetime2 NULL,
    [ApplicationUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_Tasks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tasks_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Tasks_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Tasks_Users_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [HabitCompletions] (
    [Id] uniqueidentifier NOT NULL,
    [HabitId] uniqueidentifier NOT NULL,
    [CompletionDate] date NOT NULL,
    [CompletedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [Notes] nvarchar(500) NULL,
    CONSTRAINT [PK_HabitCompletions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HabitCompletions_Habits_HabitId] FOREIGN KEY ([HabitId]) REFERENCES [Habits] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [BrainDumpItems] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Content] nvarchar(500) NOT NULL,
    [IsProcessed] bit NOT NULL DEFAULT CAST(0 AS bit),
    [ProcessedAt] datetime2 NULL,
    [ConvertedToTaskId] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [ApplicationUserId] uniqueidentifier NULL,
    [TaskId] uniqueidentifier NULL,
    CONSTRAINT [PK_BrainDumpItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BrainDumpItems_Tasks_ConvertedToTaskId] FOREIGN KEY ([ConvertedToTaskId]) REFERENCES [Tasks] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_BrainDumpItems_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([Id]),
    CONSTRAINT [FK_BrainDumpItems_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BrainDumpItems_Users_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [FocusSessions] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [TaskId] uniqueidentifier NULL,
    [StartedAt] datetime2 NOT NULL,
    [EndedAt] datetime2 NULL,
    [PlannedDuration] int NOT NULL,
    [ActualDuration] int NULL,
    [FocusLevel] int NOT NULL DEFAULT 2,
    [WhiteNoiseUsed] nvarchar(50) NULL,
    [DistractionCount] int NOT NULL DEFAULT 0,
    [WasCompleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [ApplicationUserId] uniqueidentifier NULL,
    [TaskId1] uniqueidentifier NULL,
    CONSTRAINT [PK_FocusSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FocusSessions_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_FocusSessions_Tasks_TaskId1] FOREIGN KEY ([TaskId1]) REFERENCES [Tasks] ([Id]),
    CONSTRAINT [FK_FocusSessions_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_FocusSessions_Users_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [Reminders] (
    [Id] uniqueidentifier NOT NULL,
    [TaskId] uniqueidentifier NOT NULL,
    [RemindAt] datetime2 NOT NULL,
    [IsSent] bit NOT NULL DEFAULT CAST(0 AS bit),
    [SentAt] datetime2 NULL,
    [ReminderType] nvarchar(20) NOT NULL DEFAULT N'notification',
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_Reminders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Reminders_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [DistractionLogs] (
    [Id] uniqueidentifier NOT NULL,
    [FocusSessionId] uniqueidentifier NOT NULL,
    [LoggedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [DistractionType] nvarchar(100) NULL,
    [Notes] nvarchar(200) NULL,
    CONSTRAINT [PK_DistractionLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DistractionLogs_FocusSessions_FocusSessionId] FOREIGN KEY ([FocusSessionId]) REFERENCES [FocusSessions] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_AnalyticsSnapshots_SnapshotDate] ON [AnalyticsSnapshots] ([SnapshotDate]);

CREATE INDEX [IX_AnalyticsSnapshots_UserId] ON [AnalyticsSnapshots] ([UserId]);

CREATE UNIQUE INDEX [IX_AnalyticsSnapshots_UserId_SnapshotDate] ON [AnalyticsSnapshots] ([UserId], [SnapshotDate]);

CREATE INDEX [IX_AnalyticsSnapshots_UserId1] ON [AnalyticsSnapshots] ([UserId1]);

CREATE INDEX [IX_BrainDumpItems_ApplicationUserId] ON [BrainDumpItems] ([ApplicationUserId]);

CREATE INDEX [IX_BrainDumpItems_ConvertedToTaskId] ON [BrainDumpItems] ([ConvertedToTaskId]);

CREATE INDEX [IX_BrainDumpItems_TaskId] ON [BrainDumpItems] ([TaskId]);

CREATE INDEX [IX_BrainDumpItems_UserId] ON [BrainDumpItems] ([UserId]);

CREATE INDEX [IX_BrainDumpItems_UserId_CreatedAt] ON [BrainDumpItems] ([UserId], [CreatedAt]);

CREATE INDEX [IX_BrainDumpItems_UserId_IsProcessed] ON [BrainDumpItems] ([UserId], [IsProcessed]);

CREATE INDEX [IX_Categories_ApplicationUserId] ON [Categories] ([ApplicationUserId]);

CREATE INDEX [IX_Categories_UserId] ON [Categories] ([UserId]);

CREATE INDEX [IX_Categories_UserId_OrderIndex] ON [Categories] ([UserId], [OrderIndex]);

CREATE INDEX [IX_DistractionLogs_DistractionType] ON [DistractionLogs] ([DistractionType]) WHERE [DistractionType] IS NOT NULL;

CREATE INDEX [IX_DistractionLogs_FocusSessionId] ON [DistractionLogs] ([FocusSessionId]);

CREATE INDEX [IX_FocusSessions_ApplicationUserId] ON [FocusSessions] ([ApplicationUserId]);

CREATE INDEX [IX_FocusSessions_TaskId] ON [FocusSessions] ([TaskId]);

CREATE INDEX [IX_FocusSessions_TaskId1] ON [FocusSessions] ([TaskId1]);

CREATE INDEX [IX_FocusSessions_UserId] ON [FocusSessions] ([UserId]);

CREATE INDEX [IX_FocusSessions_UserId_StartedAt] ON [FocusSessions] ([UserId], [StartedAt]);

CREATE INDEX [IX_FocusSessions_UserId_WasCompleted] ON [FocusSessions] ([UserId], [WasCompleted]);

CREATE INDEX [IX_HabitCompletions_CompletionDate] ON [HabitCompletions] ([CompletionDate]);

CREATE INDEX [IX_HabitCompletions_HabitId] ON [HabitCompletions] ([HabitId]);

CREATE UNIQUE INDEX [IX_HabitCompletions_HabitId_CompletionDate] ON [HabitCompletions] ([HabitId], [CompletionDate]);

CREATE INDEX [IX_Habits_ApplicationUserId] ON [Habits] ([ApplicationUserId]);

CREATE INDEX [IX_Habits_UserId] ON [Habits] ([UserId]);

CREATE INDEX [IX_Habits_UserId_IsActive] ON [Habits] ([UserId], [IsActive]);

CREATE INDEX [IX_Reminders_IsSent_RemindAt] ON [Reminders] ([IsSent], [RemindAt]) WHERE [IsSent] = 0;

CREATE INDEX [IX_Reminders_TaskId] ON [Reminders] ([TaskId]);

CREATE INDEX [IX_RoleClaims_RoleId] ON [RoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [Roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_Tasks_ApplicationUserId] ON [Tasks] ([ApplicationUserId]);

CREATE INDEX [IX_Tasks_CategoryId] ON [Tasks] ([CategoryId]) WHERE [CategoryId] IS NOT NULL;

CREATE INDEX [IX_Tasks_DeletedAt] ON [Tasks] ([DeletedAt]) WHERE [DeletedAt] IS NULL;

CREATE INDEX [IX_Tasks_IsCompleted] ON [Tasks] ([IsCompleted]);

CREATE INDEX [IX_Tasks_UserId_CompletedAt] ON [Tasks] ([UserId], [CompletedAt]) WHERE [CompletedAt] IS NOT NULL;

CREATE INDEX [IX_Tasks_UserId_ScheduledDate] ON [Tasks] ([UserId], [ScheduledDate]);

CREATE INDEX [IX_UserClaims_UserId] ON [UserClaims] ([UserId]);

CREATE INDEX [IX_UserLogins_UserId] ON [UserLogins] ([UserId]);

CREATE INDEX [IX_UserPreferences_ApplicationUserId] ON [UserPreferences] ([ApplicationUserId]);

CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]) WHERE [Email] IS NOT NULL;

CREATE INDEX [IX_Users_GoogleId] ON [Users] ([GoogleId]) WHERE [GoogleId] IS NOT NULL;

CREATE INDEX [IX_Users_IsActive] ON [Users] ([IsActive]);

CREATE INDEX [IX_Users_RefreshToken] ON [Users] ([RefreshToken]) WHERE [RefreshToken] IS NOT NULL;

CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260125064854_InitialCreate', N'9.0.2');

COMMIT;
GO

