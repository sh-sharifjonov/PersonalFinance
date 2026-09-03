using System.Net;
using System.Net.Http.Headers;

namespace PersonalFinance.Client.Auth;

public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly AuthService _authService;

    public AuthorizationMessageHandler(AuthService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Buffer the body up front: once sent, an HttpRequestMessage instance can't be
        // reused, so a retry after a token refresh needs its own copy of the content.
        var body = request.Content is null
            ? null
            : await request.Content.ReadAsByteArrayAsync(cancellationToken);

        await AttachTokenAsync(request);
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized || !await _authService.RefreshAsync())
        {
            return response;
        }

        response.Dispose();

        var retry = CloneRequest(request, body);
        await AttachTokenAsync(retry);
        return await base.SendAsync(retry, cancellationToken);
    }

    private async Task AttachTokenAsync(HttpRequestMessage request)
    {
        var token = await _authService.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage original, byte[]? body)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version
        };

        foreach (var header in original.Headers)
        {
            if (header.Key != "Authorization")
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (body is not null)
        {
            var content = new ByteArrayContent(body);
            if (original.Content is not null)
            {
                foreach (var header in original.Content.Headers)
                {
                    content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            clone.Content = content;
        }

        return clone;
    }
}
