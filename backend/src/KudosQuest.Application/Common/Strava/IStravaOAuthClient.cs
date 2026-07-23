namespace KudosQuest.Application.Common.Strava;

public interface IStravaOAuthClient
{
    string BuildAuthorizeUrl(string state);
    Task<StravaTokenResponse> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<StravaTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(
        string token,
        string tokenTypeHint = "refresh_token",
        CancellationToken cancellationToken = default
    );
}
