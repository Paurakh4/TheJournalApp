using Reflecta.Models;

namespace Reflecta.Services.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Register a new user
    /// </summary>
    Task<(bool Success, string Message, User? User)> RegisterAsync(string username, string email, string password, string? firstName = null, string? lastName = null);

    /// <summary>
    /// Authenticate a user with username/email and password
    /// </summary>
    Task<(bool Success, string Message, User? User)> LoginAsync(string usernameOrEmail, string password);

    /// <summary>
    /// Get current authenticated user
    /// </summary>
    Task<User?> GetCurrentUserAsync();

    /// <summary>
    /// Get user by ID
    /// </summary>
    Task<User?> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Update user profile
    /// </summary>
    Task<(bool Success, string Message)> UpdateProfileAsync(Guid userId, string? firstName, string? lastName, string? email);

    /// <summary>
    /// Change user password
    /// </summary>
    Task<(bool Success, string Message)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);

    /// <summary>
    /// Validate and set app PIN
    /// </summary>
    Task<(bool Success, string Message)> SetPinAsync(Guid userId, string pin);

    /// <summary>
    /// Verify app PIN
    /// </summary>
    Task<bool> VerifyPinAsync(Guid userId, string pin);

    /// <summary>
    /// Remove app PIN
    /// </summary>
    Task<(bool Success, string Message)> RemovePinAsync(Guid userId);

    /// <summary>
    /// Logout current user
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Current user ID (null if not authenticated)
    /// </summary>
    Guid? CurrentUserId { get; }
}
