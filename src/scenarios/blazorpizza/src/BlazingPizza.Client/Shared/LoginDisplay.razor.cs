using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace BlazingPizza.Client.Shared;

public partial class LoginDisplay
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

#if !NET11_0_OR_GREATER
    [Inject]
    private SignOutSessionStateManager SignOutManager { get; set; } = default!;
#endif

#if NET11_0_OR_GREATER
    private void BeginSignOut()
    {
        Navigation.NavigateToLogout("authentication/logout");
    }
#else
    private async Task BeginSignOut()
    {
        await SignOutManager.SetSignOutState();
        Navigation.NavigateTo("authentication/logout");
    }
#endif
}
