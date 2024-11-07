using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace ElsaStudioBlazorServer.Helpers;

public class AdAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    private static readonly AuthenticationState EmptyAuthState = new(new());

    public AdAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user is null)
            return EmptyAuthState;

        return user.Identity?.IsAuthenticated ?? false
            ? new(user)
            : EmptyAuthState;
    }

    /// <summary>
    /// Notifies the authentication state has changed.
    /// </summary>
    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}