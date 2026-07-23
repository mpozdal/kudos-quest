using KudosQuest.Application.Common.Auth;

namespace KudosQuest.Application.Common.Strava;

public sealed class StravaAccessTokenProvider(
    IStravaTokenStore tokenStore,
    IStravaOAuthClient oauthClient
) : IStravaAccessTokenProvider
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(5);

    public async Task<string?> GetValidTokenAsync(
        long athleteId,
        CancellationToken cancellationToken = default
    )
    {
        var stored = await tokenStore.GetAsync(athleteId, cancellationToken);

        if (stored is null)
        {
            return null;
        }

        if (stored.ExpiresAt > DateTimeOffset.UtcNow.Add(RefreshSkew))
        {
            return stored.AccessToken;
        }

        var refreshed = await oauthClient.RefreshTokenAsync(stored.RefreshToken, cancellationToken);

        var updated = new StoredStravaTokens
        {
            AthleteId = stored.AthleteId,
            AccessToken = refreshed.AccessToken,
            RefreshToken = string.IsNullOrWhiteSpace(refreshed.RefreshToken)
                ? stored.RefreshToken
                : refreshed.RefreshToken,
            ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(refreshed.ExpiresAt),
            Scope = stored.Scope,
            FirstName = stored.FirstName,
            LastName = stored.LastName,
            Profile = stored.Profile,
        };

        await tokenStore.SaveAsync(updated, cancellationToken);

        return updated.AccessToken;
    }
}
