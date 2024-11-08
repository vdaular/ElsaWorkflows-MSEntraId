using Elsa.Studio.Core.BlazorServer.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Designer.Extensions;
using ElsaStudioBlazorServer.Helpers;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Elsa.Studio.Secrets;
using Elsa.Studio.Models;
using Elsa.Studio.WorkflowContexts.Extensions;
using Elsa.Studio.Webhooks.Extensions;
using Elsa.Studio.Localization.Time.Providers;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Counter.Extensions;
using Elsa.Studio.DomInterop.Extensions;

// Build the host.
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services
    .AddAuthenticationCore()
    .AddOptions()
    .AddHttpContextAccessor()
    .AddScoped<ElsaBackendAuthenticationHandler>()
    .AddScoped<AuthenticationStateProvider, AdAuthenticationStateProvider>()
    .AddScoped<IUnauthorizedComponentProvider, RedirectToMicrosoftEntraIDLoginComponentProvider>()
    .AddScoped<IBackendJwtHandler, ElsaBackendJwtHandler>()
    .AddMemoryCache()
;

builder.Services.AddCircuitServicesAccessor();

builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(configuration)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("ElsaBackEnd", configuration.GetSection("ElsaBackEnd"))
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization(options => options.FallbackPolicy = options.DefaultPolicy);

builder.Services
    .AddRazorPages()
    .AddMicrosoftIdentityUI();
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents(
        circuitOptions => circuitOptions.RootComponents.RegisterCustomElsaStudioElements())
    .AddMicrosoftIdentityConsentHandler();

builder.Services.AddServerSideBlazor(options =>
{
    options.RootComponents.MaxJSRootComponents = 1000;
});

// Register shell services and modules.
// Register shell services and modules.
var backendApiConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => configuration.GetSection("Backend").Bind(options),
    ConfigureHttpClientBuilder = elsaClient => elsaClient.AuthenticationHandler = typeof(ElsaBackendAuthenticationHandler)
};
builder.Services.AddCore();
builder.Services.AddShell(options => configuration.GetSection("Shell").Bind(options));
builder.Services.AddRemoteBackend(backendApiConfig);
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();
builder.Services.AddWorkflowContextsModule();
builder.Services.AddWebhooksModule();
builder.Services.AddAgentsModule(backendApiConfig);
builder.Services.AddSecretsModule(backendApiConfig);
builder.Services.AddDomInterop();


builder.Services.AddScoped<IFeature, ElsaStudioBlazorServer.Helpers.Feature>();

// Replace some services with other implementations.
builder.Services.AddScoped<ITimeZoneProvider, LocalTimeZoneProvider>();

// Configure SignalR.
builder.Services.AddSignalR(options =>
{
    // Set MaximumReceiveMessageSize to handle large workflows.
    options.MaximumReceiveMessageSize = 5 * 1024 * 1000; // 5MB
});

// Build the application.
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapControllers();

// Run the application.
app.Run();