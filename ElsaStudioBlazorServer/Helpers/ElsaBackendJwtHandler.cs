using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace ElsaStudioBlazorServer.Helpers;

public interface IBackendJwtHandler
{
    ValueTask TrySaveTokenAsync();
    bool TryGetToken(out string? token);
}

public class ElsaBackendJwtHandler : IBackendJwtHandler
{
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _memoryCache;
    private readonly IConfiguration _configuration;
    private const string ScopesKey = "ElsaBackend:Scopes";

    public ElsaBackendJwtHandler(ITokenAcquisition tokenAcquisition,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache memoryCache,
        IConfiguration configuration)
    {
        _tokenAcquisition = tokenAcquisition;
        _httpContextAccessor = httpContextAccessor;
        _memoryCache = memoryCache;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async ValueTask TrySaveTokenAsync()
    {
        if (_httpContextAccessor.HttpContext?.User is not { Identity.IsAuthenticated: true } user)
            return;
        var key = GetKey(user);
        var scopes = _configuration.GetSection(ScopesKey).Get<string[]>()!;
        var result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(scopes);

        _memoryCache.Set(key, result.AccessToken, result.ExpiresOn);
    }

    /// <inheritdoc />
    public bool TryGetToken(out string? token)
    {
        token = null;
        if (_httpContextAccessor.HttpContext?.User is not { Identity.IsAuthenticated: true } user)
            return false;
        var key = GetKey(user);
        return _memoryCache.TryGetValue(key, out token);
    }

    private string GetKey(ClaimsPrincipal user) =>
        $"{user.GetLoginHint()}-{user.GetObjectId()}-{user.GetHomeTenantId()}";
}