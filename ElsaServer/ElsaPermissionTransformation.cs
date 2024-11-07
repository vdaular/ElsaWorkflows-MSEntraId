using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace ElsaServer;

public class ElsaPermissionTransformation : IClaimsTransformation
{
    /// <inheritdoc />
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity claimsIdentity)
            return Task.FromResult(principal);

        if (principal.IsInRole("Elsa.Core.Admin") || principal.IsInRole("Elsa.Core.App.Admin"))
            claimsIdentity.AddClaim(new("permissions", "*"));

        return Task.FromResult(principal);
    }
}
