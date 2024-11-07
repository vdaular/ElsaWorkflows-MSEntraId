namespace ElsaStudioBlazorServer.Helpers;

public class ElsaBackendAuthenticationHandler : DelegatingHandler
{
    private readonly IBackendJwtHandler _backendJwtHandler;

    public ElsaBackendAuthenticationHandler(IBackendJwtHandler backendJwtHandler)
    {
        _backendJwtHandler = backendJwtHandler;
    }


    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!_backendJwtHandler.TryGetToken(out var token))
            return await base.SendAsync(request, cancellationToken);

        const string schema = "Bearer";
        var sanitizedToken = token!.StartsWith(schema) ? token[(schema.Length + 1)..] : token;
        request.Headers.Authorization = new(schema, sanitizedToken);

        return await base.SendAsync(request, cancellationToken);
    }
}