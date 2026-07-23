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

    public async Task<StravaTokenResponse> ExchangeCodeAsync(
        string code,
        CancellationToken cancellationToken = default
    )
    {
        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
            }
        );

        using var response = await httpClient.PostAsync(_options.TokenUrl, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Strava token exchange failed ({(int)response.StatusCode}): {errorBody}"
            );
        }

        var token =
            await response.Content.ReadFromJsonAsync<StravaTokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Strava token response was empty.");

        if (string.IsNullOrWhiteSpace(token.AccessToken) || token.Athlete is null)
        {
            throw new InvalidOperationException("Strava token response was incomplete.");
        }

        return token;
    }
}
