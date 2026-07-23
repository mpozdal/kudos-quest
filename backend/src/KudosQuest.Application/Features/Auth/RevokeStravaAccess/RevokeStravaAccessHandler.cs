using System.Security.Claims;
using KudosQuest.Application.Common.Auth;
using KudosQuest.Application.Common.Strava;
using Microsoft.Extensions.Logging;

namespace KudosQuest.Application.Features.Auth.RevokeStravaAccess;

public sealed class RevokeStravaAccessHandler(
    IStravaTokenStore tokenStore,
    IStravaOAuthClient stravaOAuthClient,
    ILogger<RevokeStravaAccessHandler> logger
)
{
    public async Task HandleAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!user.TryGetAthleteId(out var athleteId))
        {
            throw new UnauthorizedAccessException("Invalid token subject.");
        }

        var stored = await tokenStore.GetAsync(athleteId, cancellationToken);
        if (stored is null)
        {
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(stored.RefreshToken))
            {
                await stravaOAuthClient.RevokeAsync(
                    stored.RefreshToken,
                    "refresh_token",
                    cancellationToken
                );
            }
            else
            {
                await stravaOAuthClient.RevokeAsync(
                    stored.AccessToken,
                    "access_token",
                    cancellationToken
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to revoke Strava token for athlete {AthleteId}. Clearing local credentials anyway.",
                athleteId
            );
        }

        await tokenStore.DeleteAsync(athleteId, cancellationToken);
    }
}
