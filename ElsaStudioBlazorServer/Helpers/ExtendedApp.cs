using Elsa.Studio.Contracts;
using Elsa.Studio.Services;
using Elsa.Studio.Shell;
using Microsoft.AspNetCore.Components;
using Microsoft.Identity.Web;

namespace ElsaStudioBlazorServer.Helpers;

public class ExtendedApp : App
{
    /// <summary>
    /// Gets or sets the <see cref="NavigationManager"/>.
    /// </summary>
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    [Inject] protected MicrosoftIdentityConsentAndConditionalAccessHandler ConsentHandler { get; set; } = default!;
    [Inject] protected IBackendJwtHandler BackendJwtHandler { get; set; } = default!;

    [Inject] protected CircuitServicesAccessor CircuitServicesAccessor { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var httpContext = CircuitServicesAccessor?.Services?.GetRequiredService<IHttpContextAccessor>().HttpContext;

        // Early exit if we are already redirected
        if (NavigationManager.Uri.Contains("/MicrosoftIdentity/Account/Challenge"))
            await base.OnInitializedAsync();

        try
        {
            await BackendJwtHandler.TrySaveTokenAsync();
        }
        catch (Exception ex)
        {
            ConsentHandler.HandleException(ex);
        }

        await base.OnInitializedAsync();
    }
}
