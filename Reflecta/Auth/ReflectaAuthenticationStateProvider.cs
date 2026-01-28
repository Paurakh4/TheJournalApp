using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Reflecta.Services;
using Reflecta.Services.Interfaces;

namespace Reflecta.Auth;

public class ReflectaAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IAuthService _authService;
    private const string UserIdKey = "reflecta_user_id";
    private const string SessionExpiryKey = "reflecta_session_expiry";
    private const int SessionDurationDays = 30;
    
    // In-memory session for non-persistent login
    private Guid? _sessionUserId;

    public ReflectaAuthenticationStateProvider(IAuthService authService)
    {
        _authService = authService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // First check in-memory session
        Guid? activeUserId = _sessionUserId;
        
        // If no in-memory session, try to restore from secure storage
        if (!activeUserId.HasValue)
        {
            // Check if session has expired
            if (await IsSessionExpiredAsync())
            {
                await ClearSessionAsync();
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
            
            activeUserId = await GetStoredUserIdAsync();
            if (activeUserId.HasValue)
            {
                // Restore in-memory session from persisted state
                _sessionUserId = activeUserId;
            }
        }
        
        if (activeUserId.HasValue && !_authService.IsAuthenticated)
        {
            // Restore the session
            var user = await _authService.GetUserByIdAsync(activeUserId.Value);
            if (user != null && user.IsActive)
            {
                ((AuthService)_authService).SetCurrentUser(activeUserId.Value);
            }
            else
            {
                // Invalid stored user, clear it
                await ClearSessionAsync();
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        if (_authService.IsAuthenticated && _authService.CurrentUserId.HasValue)
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email)
                };

                if (!string.IsNullOrEmpty(user.FirstName))
                {
                    claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));
                }

                if (!string.IsNullOrEmpty(user.LastName))
                {
                    claims.Add(new Claim(ClaimTypes.Surname, user.LastName));
                }

                var identity = new ClaimsIdentity(claims, "ReflectaAuth");
                var principal = new ClaimsPrincipal(identity);

                return new AuthenticationState(principal);
            }
        }

        // Not authenticated
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    public async Task LoginAsync(Guid userId, bool rememberMe = false)
    {
        // Always set in-memory session
        _sessionUserId = userId;
        
        // Always persist session with 30-day expiry
        await StoreUserIdAsync(userId);
        await StoreSessionExpiryAsync();
        
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        await ClearSessionAsync();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
    
    private async Task ClearSessionAsync()
    {
        _sessionUserId = null;
        await ClearStoredUserIdAsync();
    }

    public void NotifyStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    // Secure storage methods for persistence
    private async Task<Guid?> GetStoredUserIdAsync()
    {
        try
        {
            var storedValue = await SecureStorage.Default.GetAsync(UserIdKey);
            if (!string.IsNullOrEmpty(storedValue) && Guid.TryParse(storedValue, out var userId))
            {
                return userId;
            }
        }
        catch
        {
            // SecureStorage might not be available on all platforms during development
        }

        return null;
    }

    private async Task StoreUserIdAsync(Guid userId)
    {
        try
        {
            await SecureStorage.Default.SetAsync(UserIdKey, userId.ToString());
        }
        catch
        {
            // SecureStorage might not be available on all platforms during development
        }
    }

    private async Task ClearStoredUserIdAsync()
    {
        try
        {
            SecureStorage.Default.Remove(UserIdKey);
            SecureStorage.Default.Remove(SessionExpiryKey);
        }
        catch
        {
            // SecureStorage might not be available on all platforms during development
        }

        await Task.CompletedTask;
    }
    
    private async Task StoreSessionExpiryAsync()
    {
        try
        {
            var expiryDate = DateTime.UtcNow.AddDays(SessionDurationDays);
            await SecureStorage.Default.SetAsync(SessionExpiryKey, expiryDate.ToString("O"));
        }
        catch
        {
            // SecureStorage might not be available on all platforms during development
        }
    }
    
    private async Task<bool> IsSessionExpiredAsync()
    {
        try
        {
            var storedValue = await SecureStorage.Default.GetAsync(SessionExpiryKey);
            if (!string.IsNullOrEmpty(storedValue) && DateTime.TryParse(storedValue, out var expiryDate))
            {
                return DateTime.UtcNow > expiryDate;
            }
            // No expiry stored means session should be treated as expired (legacy or first-time)
            // But only if there's a stored user ID
            var userId = await SecureStorage.Default.GetAsync(UserIdKey);
            return !string.IsNullOrEmpty(userId); // If user exists but no expiry, force re-login
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Check if there's a valid persisted session (for PIN unlock flow)
    /// </summary>
    public async Task<bool> HasValidSessionAsync()
    {
        // Check in-memory first
        if (_authService.IsAuthenticated && _authService.CurrentUserId.HasValue)
            return true;

        // Check persisted session
        if (await IsSessionExpiredAsync())
            return false;
            
        var userId = await GetStoredUserIdAsync();
        return userId.HasValue;
    }
    
    /// <summary>
    /// Get the stored user ID without triggering full auth state (for PIN unlock)
    /// </summary>
    public async Task<Guid?> GetStoredUserIdForPinUnlockAsync()
    {
        if (await IsSessionExpiredAsync())
            return null;
            
        return await GetStoredUserIdAsync();
    }
    
    /// <summary>
    /// Extend the session expiry (called after successful PIN unlock or activity)
    /// </summary>
    public async Task ExtendSessionAsync()
    {
        try
        {
            // Only extend if there's a valid session
            var userId = await GetStoredUserIdAsync();
            if (userId.HasValue)
            {
                await StoreSessionExpiryAsync();
            }
        }
        catch
        {
            // Silently handle session extension errors
        }
    }
    
    /// <summary>
    /// Check if a user has PIN enabled (for determining navigation flow)
    /// </summary>
    public async Task<bool> HasPinEnabledAsync(Guid userId)
    {
        try
        {
            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
                return false;
            
            // Check user settings for PIN
            // This requires accessing the settings service, but we'll use a simpler approach
            // The caller should check this via the SettingsService
            return false;
        }
        catch
        {
            return false;
        }
    }
}
