using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;

namespace KudosQuest.Application.Common.Strava;

public sealed class StravaOAuthClient(HttpClient httpClient, IOptions<StravaOptions> options)
    : IStravaOAuthClient
{
    private readonly StravaOptions _options = options.Value;

    public string BuildAuthorizeUrl(string state)
    {
        var query = new StringBuilder();
        query.Append("client_id=").Append(Uri.EscapeDataString(_options.ClientId));
        query.Append("&redirect_uri=").Append(Uri.EscapeDataString(_options.RedirectUri));
        query.Append("&response_type=code");
        query.Append("&approval_prompt=auto");
        query.Append("&scope=").Append(Uri.EscapeDataString(_options.Scopes));
        query.Append("&state=").Append(Uri.EscapeDataString(state));

        return $"{_options.AuthorizeUrl}?{query}";
    }

    public Task<StravaTokenResponse> ExchangeCodeAsync(
        string code,
        CancellationToken cancellationToken = default
    ) =>
        PostTokenAsync(
            "token exchange",
            new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
            },
            requireAthlete: true,
            cancellationToken
        );

    public Task<StravaTokenResponse> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default
    ) =>
        PostTokenAsync(
            "token refresh",
            new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
            },
            requireAthlete: false,
            cancellationToken
        );

    public async Task RevokeAsync(
        string token,
        string tokenTypeHint = "refresh_token",
        CancellationToken cancellationToken = default
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.RevokeUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}")
            )
        );
        request.Content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["token"] = token,
                ["token_type_hint"] = tokenTypeHint,
            }
        );

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "token revoke", cancellationToken);
    }

    private async Task<StravaTokenResponse> PostTokenAsync(
        string operation,
        IReadOnlyDictionary<string, string> fields,
        bool requireAthlete,
        CancellationToken cancellationToken
    )
    {
        using var response = await PostFormAsync(_options.TokenUrl, fields, cancellationToken);
        await EnsureSuccessAsync(response, operation, cancellationToken);

        var token =
            await response.Content.ReadFromJsonAsync<StravaTokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException($"Strava {operation} response was empty.");

        if (string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException($"Strava {operation} response was incomplete.");
        }

        if (requireAthlete && token.Athlete is null)
        {
            throw new InvalidOperationException($"Strava {operation} response missing athlete.");
        }

        return token;
    }

    private async Task<HttpResponseMessage> PostFormAsync(
        string url,
        IReadOnlyDictionary<string, string> fields,
        CancellationToken cancellationToken
    )
    {
        var payload = new Dictionary<string, string>(fields)
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
        };

        using var content = new FormUrlEncodedContent(payload);
        return await httpClient.PostAsync(url, content, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken
    )
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"Strava {operation} failed ({(int)response.StatusCode}): {errorBody}"
        );
    }
}
