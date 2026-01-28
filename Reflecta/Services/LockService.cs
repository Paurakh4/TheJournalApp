using Microsoft.EntityFrameworkCore;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using Reflecta.Data;
using Reflecta.Services.Interfaces;
using BCrypt.Net;

namespace Reflecta.Services;

/// <summary>
/// Singleton service to manage app lock state for PIN/biometric protection
/// </summary>
public class LockService : ILockService
{
    private readonly IDbContextFactory<ReflectaDbContext> _contextFactory;
    
    // SecureStorage keys for persistent state
    private const string LockRequiredKey = "reflecta_lock_required";
    private const string FreshLoginKey = "reflecta_fresh_login";
    private const string FreshLoginExpiryKey = "reflecta_fresh_login_expiry";
    
    private bool _isLocked;
    private bool _isPinLockEnabled;
    private bool _isBiometricEnabled;
    private int _lockTimeoutMinutes;
    private DateTime? _backgroundTime;
    private DateTime? _lastUnlockTime;
    private Guid? _currentUserId;
    private bool _isFreshLogin;
    private bool _hasUnlockedThisSession; // Tracks if user unlocked with PIN/biometric this session
    private bool _isTestMode; // Test mode for testing PIN/biometric from settings
    
    public bool IsLocked => _isLocked;
    public bool IsPinLockEnabled => _isPinLockEnabled;
    public bool IsBiometricEnabled => _isBiometricEnabled;
    public int LockTimeoutMinutes => _lockTimeoutMinutes;
    public bool IsFreshLogin => _isFreshLogin;
    public bool IsTestMode => _isTestMode;
    
    public event Action? OnLockStateChanged;

    public LockService(IDbContextFactory<ReflectaDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task InitializeAsync(Guid userId)
    {
        _currentUserId = userId;
        await RefreshSettingsAsync();
        
        // If user already unlocked this session (via PIN/biometric), don't re-lock
        if (_hasUnlockedThisSession)
        {
            _isLocked = false;
            return;
        }
        
        // Check if this is a fresh login - prioritize in-memory state (singleton),
        // then fall back to SecureStorage check
        if (!_isFreshLogin)
        {
            _isFreshLogin = await CheckFreshLoginFlagAsync();
        }
        
        // If fresh login, don't lock - user just authenticated
        if (_isFreshLogin)
        {
            _isLocked = false;
            return;
        }
        
        // Check if we should lock on cold start
        var shouldLockOnColdStart = await ShouldLockOnColdStartAsync();
        
        // Lock if PIN is enabled and either:
        // 1. Already marked as locked
        // 2. Should lock on resume based on timeout
        // 3. Cold start scenario (app was closed and reopened)
        if (_isPinLockEnabled && (_isLocked || ShouldLockOnResume() || shouldLockOnColdStart))
        {
            _isLocked = true;
            OnLockStateChanged?.Invoke();
        }
    }

    public async Task InitializeAfterFreshLoginAsync(Guid userId)
    {
        _currentUserId = userId;
        await RefreshSettingsAsync();
        
        // Mark as fresh login - user just authenticated with password
        _isFreshLogin = true;
        await SetFreshLoginFlagAsync();
        
        // Don't lock after fresh login
        _isLocked = false;
        _lastUnlockTime = DateTime.UtcNow;
        
        // Clear the lock required state since user just authenticated
        await ClearLockRequiredStateAsync();
    }

    public void LockApp()
    {
        if (_isPinLockEnabled && !_isLocked && !_isFreshLogin)
        {
            _isLocked = true;
            OnLockStateChanged?.Invoke();
        }
    }

    public async Task<bool> UnlockWithPinAsync(string pin)
    {
        if (!_currentUserId.HasValue)
        {
            return false;
        }
        
        await using var context = await _contextFactory.CreateDbContextAsync();
        var settings = await context.UserSettings.FirstOrDefaultAsync(s => s.UserId == _currentUserId.Value);
        
        if (settings?.PinHash == null || !settings.PinEnabled)
        {
            return true; // No PIN required
        }
        
        var isValid = BCrypt.Net.BCrypt.Verify(pin, settings.PinHash);
        
        if (isValid)
        {
            _isLocked = false;
            _backgroundTime = null;
            _lastUnlockTime = DateTime.UtcNow;
            _hasUnlockedThisSession = true; // Mark that user unlocked this session
            
            // Clear the fresh login flag and lock required state
            await ClearFreshLoginFlagAsync();
            await ClearLockRequiredStateAsync();
            
            OnLockStateChanged?.Invoke();
        }
        
        return isValid;
    }

    public async Task<bool> UnlockWithBiometricAsync()
    {
        if (!_isBiometricEnabled || !_isPinLockEnabled)
            return false;
            
        try
        {
            // Check if biometric authentication is available
            var isAvailable = await CrossFingerprint.Current.IsAvailableAsync();
            if (!isAvailable)
                return false;
                
            // Configure the authentication dialog
            var config = new AuthenticationRequestConfiguration(
                "Unlock Reflecta",
                "Use your fingerprint or face to unlock your journal")
            {
                FallbackTitle = "Use PIN",
                CancelTitle = "Cancel",
                AllowAlternativeAuthentication = false // Don't allow device passcode, require biometric
            };
            
            // Perform biometric authentication
            var result = await CrossFingerprint.Current.AuthenticateAsync(config);
            
            if (result.Authenticated)
            {
                _isLocked = false;
                _backgroundTime = null;
                _lastUnlockTime = DateTime.UtcNow;
                _hasUnlockedThisSession = true; // Mark that user unlocked this session
                
                // Clear the fresh login flag and lock required state
                await ClearFreshLoginFlagAsync();
                await ClearLockRequiredStateAsync();
                
                OnLockStateChanged?.Invoke();
                return true;
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }

    public bool ShouldLockOnResume()
    {
        // If PIN is not enabled, never lock
        if (!_isPinLockEnabled)
            return false;
        
        // If this is a fresh login, don't lock
        if (_isFreshLogin)
            return false;
        
        // If user already unlocked this session (without going to background), don't lock
        if (_hasUnlockedThisSession && _backgroundTime == null)
            return false;
            
        // If timeout is -1 (never), don't lock on resume
        if (_lockTimeoutMinutes == -1)
            return false;
            
        // If timeout is 0 (always), lock on every resume
        if (_lockTimeoutMinutes == 0)
            return true;

        var referenceTime = _backgroundTime ?? _lastUnlockTime;
        if (referenceTime.HasValue)
        {
            var elapsed = DateTime.UtcNow - referenceTime.Value;
            return elapsed.TotalMinutes >= _lockTimeoutMinutes;
        }
        
        // No timing info recorded, assume should lock (cold start scenario)
        return true;
    }

    public async Task<bool> ShouldLockOnColdStartAsync()
    {
        // On cold start (app was killed and reopened), we should lock if:
        // 1. PIN is enabled
        // 2. Not a fresh login
        // 3. User has unlocked before (has a valid session)
        
        if (!_isPinLockEnabled)
            return false;
            
        if (_isFreshLogin)
            return false;
        
        // If user already unlocked this session, don't lock again
        if (_hasUnlockedThisSession)
            return false;
        
        try
        {
            // Check if lock was required (persisted state)
            var lockRequired = await SecureStorage.Default.GetAsync(LockRequiredKey);
            if (!string.IsNullOrEmpty(lockRequired) && bool.TryParse(lockRequired, out var required))
            {
                return required;
            }
            
            // Default: If PIN is enabled and no specific state, require lock on cold start
            return true;
        }
        catch
        {
            // On error, default to requiring lock for security
            return _isPinLockEnabled;
        }
    }

    public async Task PersistLockStateAsync()
    {
        try
        {
            // Persist that lock is required on next app open
            await SecureStorage.Default.SetAsync(LockRequiredKey, _isPinLockEnabled.ToString());
        }
        catch
        {
            // Silently ignore storage errors
        }
    }

    public void RecordBackgroundTime()
    {
        _backgroundTime = DateTime.UtcNow;
        // Reset the unlock flag when going to background - next resume may require lock
        _hasUnlockedThisSession = false;
        
        // Also persist lock state for cold start scenarios
        _ = PersistLockStateAsync();
    }

    public void Reset()
    {
        _isLocked = false;
        _isPinLockEnabled = false;
        _isBiometricEnabled = false;
        _lockTimeoutMinutes = 0;
        _backgroundTime = null;
        _lastUnlockTime = null;
        _currentUserId = null;
        _isFreshLogin = false;
        _hasUnlockedThisSession = false;
        _isTestMode = false;
        
        // Clear persisted state
        _ = ClearAllPersistedStateAsync();
        
        OnLockStateChanged?.Invoke();
    }
    
    public void ClearFreshLoginFlag()
    {
        _isFreshLogin = false;
        _ = ClearFreshLoginFlagAsync();
    }
    
    public void EnableTestMode()
    {
        _isTestMode = true;
        _isLocked = true; // Set locked so PIN screen shows
        OnLockStateChanged?.Invoke();
    }
    
    public void DisableTestMode()
    {
        _isTestMode = false;
        _isLocked = false; // Unlock after test
        OnLockStateChanged?.Invoke();
    }

    public async Task RefreshSettingsAsync()
    {
        if (!_currentUserId.HasValue)
            return;
            
        await using var context = await _contextFactory.CreateDbContextAsync();
        var settings = await context.UserSettings.FirstOrDefaultAsync(s => s.UserId == _currentUserId.Value);
        
        if (settings != null)
        {
            _isPinLockEnabled = settings.PinEnabled && !string.IsNullOrEmpty(settings.PinHash);
            _isBiometricEnabled = settings.BiometricEnabled;
            _lockTimeoutMinutes = settings.LockTimeoutMinutes;
        }
        else
        {
            _isPinLockEnabled = false;
            _isBiometricEnabled = false;
            _lockTimeoutMinutes = 0;
        }
    }

    public async Task UpdateLockTimeoutAsync(int timeoutMinutes)
    {
        if (!_currentUserId.HasValue)
            return;
            
        await using var context = await _contextFactory.CreateDbContextAsync();
        var settings = await context.UserSettings.FirstOrDefaultAsync(s => s.UserId == _currentUserId.Value);
        
        if (settings != null)
        {
            settings.LockTimeoutMinutes = timeoutMinutes;
            settings.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            _lockTimeoutMinutes = timeoutMinutes;
        }
    }

    public async Task UpdateBiometricEnabledAsync(bool enabled)
    {
        if (!_currentUserId.HasValue)
            return;
            
        await using var context = await _contextFactory.CreateDbContextAsync();
        var settings = await context.UserSettings.FirstOrDefaultAsync(s => s.UserId == _currentUserId.Value);
        
        if (settings != null)
        {
            settings.BiometricEnabled = enabled;
            settings.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            _isBiometricEnabled = enabled;
        }
    }
    
    public async Task<bool> IsBiometricAvailableAsync()
    {
        try
        {
            return await CrossFingerprint.Current.IsAvailableAsync();
        }
        catch
        {
            return false;
        }
    }
    
    // Private helper methods for SecureStorage operations
    
    private async Task SetFreshLoginFlagAsync()
    {
        try
        {
            // Set fresh login flag with a longer expiry (30 minutes)
            // This ensures the user has enough time to set up PIN without being interrupted
            var expiry = DateTime.UtcNow.AddMinutes(30);
            await SecureStorage.Default.SetAsync(FreshLoginKey, "true");
            await SecureStorage.Default.SetAsync(FreshLoginExpiryKey, expiry.ToString("O"));
        }
        catch
        {
            // Silently ignore storage errors
        }
    }
    
    private async Task<bool> CheckFreshLoginFlagAsync()
    {
        try
        {
            var flag = await SecureStorage.Default.GetAsync(FreshLoginKey);
            if (string.IsNullOrEmpty(flag) || flag != "true")
                return false;
            
            // Check if fresh login flag has expired
            var expiryStr = await SecureStorage.Default.GetAsync(FreshLoginExpiryKey);
            if (!string.IsNullOrEmpty(expiryStr) && DateTime.TryParse(expiryStr, out var expiry))
            {
                if (DateTime.UtcNow > expiry)
                {
                    // Flag expired, clear it
                    await ClearFreshLoginFlagAsync();
                    return false;
                }
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private async Task ClearFreshLoginFlagAsync()
    {
        try
        {
            SecureStorage.Default.Remove(FreshLoginKey);
            SecureStorage.Default.Remove(FreshLoginExpiryKey);
        }
        catch
        {
            // Silently ignore storage errors
        }
        
        await Task.CompletedTask;
    }
    
    private async Task ClearLockRequiredStateAsync()
    {
        try
        {
            SecureStorage.Default.Remove(LockRequiredKey);
        }
        catch
        {
            // Silently ignore storage errors
        }
        
        await Task.CompletedTask;
    }
    
    private async Task ClearAllPersistedStateAsync()
    {
        try
        {
            SecureStorage.Default.Remove(LockRequiredKey);
            SecureStorage.Default.Remove(FreshLoginKey);
            SecureStorage.Default.Remove(FreshLoginExpiryKey);
        }
        catch
        {
            // Silently ignore storage errors
        }
        
        await Task.CompletedTask;
    }
}
