using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using Reflecta.Data;
using Reflecta.Models;
using Reflecta.Services.Interfaces;
using BCrypt.Net;

namespace Reflecta.Services;

public class AuthService : IAuthService
{
    private readonly IDbContextFactory<ReflectaDbContext> _contextFactory;
    private readonly ISettingsService _settingsService;
    private readonly IStreakService _streakService;
    
    private User? _currentUser;
    private Guid? _currentUserId;

    public bool IsAuthenticated => _currentUserId.HasValue;
    public Guid? CurrentUserId => _currentUserId;

    public AuthService(IDbContextFactory<ReflectaDbContext> contextFactory, ISettingsService settingsService, IStreakService streakService)
    {
        _contextFactory = contextFactory;
        _settingsService = settingsService;
        _streakService = streakService;
    }

    public async Task<(bool Success, string Message, User? User)> RegisterAsync(
        string username, string email, string password, string? firstName = null, string? lastName = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            // Validate input
            if (string.IsNullOrWhiteSpace(username))
                return (false, "Username is required", null);
            
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Email is required", null);
            
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password is required", null);
            
            if (password.Length < 6)
                return (false, "Password must be at least 6 characters", null);

            // Check for existing user
            var existingUser = await context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower() || 
                                          u.Email.ToLower() == email.ToLower());
            
            if (existingUser != null)
            {
                if (existingUser.Username.ToLower() == username.ToLower())
                    return (false, "Username already exists", null);
                return (false, "Email already exists", null);
            }

            // Create user
            var user = new User
            {
                Username = username,
                Email = email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FirstName = firstName,
                LastName = lastName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Create default settings
            await _settingsService.CreateDefaultSettingsAsync(user.Id);

            // Initialize streak
            var streak = new Streak
            {
                UserId = user.Id,
                CurrentStreak = 0,
                LongestStreak = 0
            };
            context.Streaks.Add(streak);
            await context.SaveChangesAsync();

            // Auto-login after registration
            _currentUser = user;
            _currentUserId = user.Id;

            return (true, "Registration successful", user);
        }
        catch (Exception ex) when (IsDatabaseException(ex))
        {
            return (false, "Database connection failed. Ensure PostgreSQL is running and the connection string is correct.", null);
        }
    }

    public async Task<(bool Success, string Message, User? User)> LoginAsync(string usernameOrEmail, string password)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            if (string.IsNullOrWhiteSpace(usernameOrEmail))
                return (false, "Username or email is required", null);
            
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password is required", null);

            var user = await context.Users
                .Include(u => u.Settings)
                .FirstOrDefaultAsync(u => u.Username.ToLower() == usernameOrEmail.ToLower() || 
                                          u.Email.ToLower() == usernameOrEmail.ToLower());

            if (user == null)
                return (false, "Invalid username or password", null);

            if (!user.IsActive)
                return (false, "Account is deactivated", null);

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return (false, "Invalid username or password", null);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            _currentUser = user;
            _currentUserId = user.Id;

            return (true, "Login successful", user);
        }
        catch (Exception ex) when (IsDatabaseException(ex))
        {
            return (false, "Database connection failed. Ensure PostgreSQL is running and the connection string is correct.", null);
        }
    }

    private static bool IsDatabaseException(Exception exception)
    {
        if (exception is DbException || exception is DbUpdateException)
            return true;

        return exception.InnerException is DbException || exception.InnerException is DbUpdateException;
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        if (!_currentUserId.HasValue)
            return null;

        if (_currentUser != null && _currentUser.Id == _currentUserId.Value)
            return _currentUser;

        await using var context = await _contextFactory.CreateDbContextAsync();
        _currentUser = await context.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.Id == _currentUserId.Value);

        return _currentUser;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<(bool Success, string Message)> UpdateProfileAsync(
        Guid userId, string? firstName, string? lastName, string? email)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync(userId);
        if (user == null)
            return (false, "User not found");

        if (!string.IsNullOrWhiteSpace(email) && email.ToLower() != user.Email.ToLower())
        {
            var emailExists = await context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower() && u.Id != userId);
            
            if (emailExists)
                return (false, "Email already in use");

            user.Email = email.ToLower();
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return (true, "Profile updated successfully");
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync(userId);
        if (user == null)
            return (false, "User not found");

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return (false, "Current password is incorrect");

        if (newPassword.Length < 6)
            return (false, "New password must be at least 6 characters");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return (true, "Password changed successfully");
    }

    public async Task<(bool Success, string Message)> SetPinAsync(Guid userId, string pin)
    {
        if (string.IsNullOrWhiteSpace(pin) || pin.Length != 4)
            return (false, "PIN must be 4 digits");

        if (!pin.All(char.IsDigit))
            return (false, "PIN must contain only digits");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var settings = await context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
        if (settings == null)
        {
            settings = new UserSettings
            {
                UserId = userId,
                Theme = "dark",
                PinEnabled = false,
                ReminderEnabled = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.UserSettings.Add(settings);
        }

        settings.PinHash = BCrypt.Net.BCrypt.HashPassword(pin);
        settings.PinEnabled = true;
        settings.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return (true, "PIN set successfully");
    }

    public async Task<bool> VerifyPinAsync(Guid userId, string pin)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var settings = await context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
        if (settings?.PinHash == null || !settings.PinEnabled)
            return true; // No PIN required

        return BCrypt.Net.BCrypt.Verify(pin, settings.PinHash);
    }

    public async Task<(bool Success, string Message)> RemovePinAsync(Guid userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var settings = await context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
        if (settings == null)
            return (false, "Settings not found");

        settings.PinHash = null;
        settings.PinEnabled = false;
        settings.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return (true, "PIN removed successfully");
    }

    public Task LogoutAsync()
    {
        _currentUser = null;
        _currentUserId = null;
        return Task.CompletedTask;
    }

    // Method to restore session (used by AuthenticationStateProvider)
    public void SetCurrentUser(Guid userId)
    {
        _currentUserId = userId;
        _currentUser = null; // Will be loaded on next GetCurrentUserAsync call
    }
}
