using Reflecta.Auth;
using Reflecta.Services.Interfaces;

namespace Reflecta;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private bool _isFirstResume = true;
    
    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new MainPage()) { Title = "Reflecta" };
        
        // Subscribe to lifecycle events
        window.Resumed += OnAppResumed;
        window.Stopped += OnAppStopped;
        window.Destroying += OnAppDestroying;
        
        return window;
    }
    
    private async void OnAppResumed(object? sender, EventArgs e)
    {
        // Lock the app when resuming from background (if PIN is enabled and timeout has elapsed)
        try
        {
            // Use the singleton LockService directly
            var lockService = _serviceProvider.GetService<ILockService>();
            if (lockService == null)
                return;

            // Create a scope for scoped services
            using var scope = _serviceProvider.CreateScope();
            var authStateProvider = scope.ServiceProvider.GetService<ReflectaAuthenticationStateProvider>();
            
            if (authStateProvider != null)
            {
                var hasValidSession = await authStateProvider.HasValidSessionAsync();
                if (!hasValidSession)
                {
                    // No valid session - reset lock service
                    lockService.Reset();
                    return;
                }

                var storedUserId = await authStateProvider.GetStoredUserIdForPinUnlockAsync();
                if (storedUserId.HasValue)
                {
                    // Initialize lock service (will check if should lock)
                    await lockService.InitializeAsync(storedUserId.Value);
                    
                    // If not first resume and should lock, lock the app
                    if (!_isFirstResume && lockService.ShouldLockOnResume())
                    {
                        lockService.LockApp();
                    }
                    // For first resume (cold start), the InitializeAsync already handles locking
                }
            }
            
            _isFirstResume = false;
        }
        catch
        {
            // Silently handle resume errors
        }
    }
    
    private async void OnAppStopped(object? sender, EventArgs e)
    {
        // Record when app goes to background for timeout calculation
        // and persist the lock state so PIN lock shows on cold start
        try
        {
            // Use the singleton LockService directly
            var lockService = _serviceProvider.GetService<ILockService>();
            if (lockService != null)
            {
                lockService.RecordBackgroundTime();
                
                // Explicitly persist lock state for cold start
                await lockService.PersistLockStateAsync();
            }
        }
        catch
        {
            // Silently handle stop errors
        }
    }
    
    private void OnAppDestroying(object? sender, EventArgs e)
    {
        // Clean up if needed
        if (sender is Window window)
        {
            window.Resumed -= OnAppResumed;
            window.Stopped -= OnAppStopped;
            window.Destroying -= OnAppDestroying;
        }
    }
}
