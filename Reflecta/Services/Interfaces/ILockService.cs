namespace Reflecta.Services.Interfaces;

/// <summary>
/// Service to manage app lock state for PIN/biometric protection
/// </summary>
public interface ILockService
{
    /// <summary>
    /// Whether the app is currently locked and requires PIN/biometric to unlock
    /// </summary>
    bool IsLocked { get; }
    
    /// <summary>
    /// Whether PIN lock is enabled for the current user
    /// </summary>
    bool IsPinLockEnabled { get; }
    
    /// <summary>
    /// Whether biometric unlock is enabled
    /// </summary>
    bool IsBiometricEnabled { get; }
    
    /// <summary>
    /// The configured lock timeout in minutes (0 = always, -1 = never)
    /// </summary>
    int LockTimeoutMinutes { get; }
    
    /// <summary>
    /// Whether the current session is from a fresh login (just entered username/password)
    /// When true, PIN lock should be skipped as user just authenticated
    /// </summary>
    bool IsFreshLogin { get; }
    
    /// <summary>
    /// Event fired when lock state changes
    /// </summary>
    event Action? OnLockStateChanged;
    
    /// <summary>
    /// Initialize the lock service for a user
    /// </summary>
    Task InitializeAsync(Guid userId);
    
    /// <summary>
    /// Initialize the lock service for a user after a fresh login (password authentication)
    /// This sets the fresh login flag to skip PIN lock requirement
    /// </summary>
    Task InitializeAfterFreshLoginAsync(Guid userId);
    
    /// <summary>
    /// Lock the app (called on app resume/start)
    /// </summary>
    void LockApp();
    
    /// <summary>
    /// Attempt to unlock with PIN
    /// </summary>
    Task<bool> UnlockWithPinAsync(string pin);
    
    /// <summary>
    /// Attempt to unlock with biometrics
    /// </summary>
    Task<bool> UnlockWithBiometricAsync();
    
    /// <summary>
    /// Check if the app should be locked based on timeout settings
    /// </summary>
    bool ShouldLockOnResume();
    
    /// <summary>
    /// Record when the app goes to background
    /// </summary>
    void RecordBackgroundTime();
    
    /// <summary>
    /// Clear lock state (on logout)
    /// </summary>
    void Reset();
    
    /// <summary>
    /// Refresh lock settings from database
    /// </summary>
    Task RefreshSettingsAsync();
    
    /// <summary>
    /// Update lock timeout setting
    /// </summary>
    Task UpdateLockTimeoutAsync(int timeoutMinutes);
    
    /// <summary>
    /// Update biometric enabled setting
    /// </summary>
    Task UpdateBiometricEnabledAsync(bool enabled);
    
    /// <summary>
    /// Check if biometric authentication is available on this device
    /// </summary>
    Task<bool> IsBiometricAvailableAsync();
    
    /// <summary>
    /// Clear the fresh login flag (called after user navigates away from login flow)
    /// </summary>
    void ClearFreshLoginFlag();
    
    /// <summary>
    /// Check if the app should require lock on cold start (app was killed and reopened)
    /// </summary>
    Task<bool> ShouldLockOnColdStartAsync();
    
    /// <summary>
    /// Persist the lock requirement state for cold start scenarios
    /// </summary>
    Task PersistLockStateAsync();
    
    /// <summary>
    /// Whether the lock screen is in test mode (testing PIN/biometric from settings)
    /// </summary>
    bool IsTestMode { get; }
    
    /// <summary>
    /// Enable test mode to test PIN/biometric from settings
    /// </summary>
    void EnableTestMode();
    
    /// <summary>
    /// Disable test mode after testing is complete
    /// </summary>
    void DisableTestMode();
}
