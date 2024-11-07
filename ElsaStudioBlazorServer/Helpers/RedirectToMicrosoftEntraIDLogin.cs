using Microsoft.AspNetCore.Components;

namespace ElsaStudioBlazorServer.Helpers
{
    public class RedirectToMicrosoftEntraIDLogin : ComponentBase
    {
        /// <summary>
        /// Gets or sets the <see cref="NavigationManager"/>.
        /// </summary>
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

        /// <inheritdoc />
        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            NavigationManager.NavigateTo("ad-login", true);
            return Task.CompletedTask;
        }
    }
}
