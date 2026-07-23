namespace KudosQuest.Application.Common.Strava;

public interface IStravaOAuthClient
{
    string BuildAuthorizeUrl(string state);
    Task<StravaTokenResponse> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);
}
