using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;

namespace ElsaStudioBlazorServer.Pages;

[AllowAnonymous]
public partial class AdLogin
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private async Task AdLoginAsync()
    {
        var returnUri = NavigationManager.ToBaseRelativePath(NavigationManager.BaseUri) + "/post-login";
        NavigationManager.NavigateTo($"/MicrosoftIdentity/Account/SignIn?redirectUri={returnUri}", true);
    }
}