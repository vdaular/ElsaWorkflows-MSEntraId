using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Microsoft.AspNetCore.Components;

namespace ElsaStudioBlazorServer.Helpers;

public class RedirectToMicrosoftEntraIDLoginComponentProvider : IUnauthorizedComponentProvider
{
    /// <inheritdoc />
    public RenderFragment GetUnauthorizedComponent()
    {
        return builder => builder.CreateComponent<RedirectToMicrosoftEntraIDLogin>();
    }
}
