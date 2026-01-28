using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Reflecta.Services.Interfaces;

namespace Reflecta.Auth;

/// <summary>
/// Component to check if user is authenticated, has PIN set up, and redirect appropriately
/// </summary>
public class AuthGuard : ComponentBase
{
    [Inject]
    private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private ILockService LockService { get; set; } = default!;

    [Inject]
    private ISettingsService SettingsService { get; set; } = default!;

    [Inject]
    private IAuthService AuthService { get; set; } = default!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string RedirectUrl { get; set; } = "/login";

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        
        // First check: Is user authenticated?
        if (authState.User.Identity?.IsAuthenticated != true)
        {
            Navigation.NavigateTo(RedirectUrl, replace: true);
            return;
        }

        // Get user ID for further checks
        var userId = AuthService.CurrentUserId;
        if (!userId.HasValue)
        {
            Navigation.NavigateTo(RedirectUrl, replace: true);
            return;
        }

        // Second check: Does user have PIN set up? (mandatory)
        var settings = await SettingsService.GetSettingsAsync(userId.Value);
        if (settings == null || !settings.PinEnabled || string.IsNullOrEmpty(settings.PinHash))
        {
            // User doesn't have PIN set up - redirect to PIN setup
            // But only if we're not already going there
            var currentUri = Navigation.ToBaseRelativePath(Navigation.Uri);
            if (!currentUri.StartsWith("pin-setup", StringComparison.OrdinalIgnoreCase))
            {
                Navigation.NavigateTo("/pin-setup?Required=true", replace: true);
                return;
            }
        }

        // Third check: Is app locked and requires PIN unlock?
        // Skip this check if it's a fresh login (user just authenticated with password)
        if (LockService.IsLocked && !LockService.IsFreshLogin)
        {
            Navigation.NavigateTo("/pin-lock", replace: true);
            return;
        }
    }
}
