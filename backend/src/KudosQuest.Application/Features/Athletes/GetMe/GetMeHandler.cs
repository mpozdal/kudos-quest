using System.Security.Claims;
using KudosQuest.Application.Common.Auth;

namespace KudosQuest.Application.Features.Athletes.GetMe;

public sealed class GetMeHandler(IStravaTokenStore tokenStore)
{
    public async Task<GetMeResponse?> HandleAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default
    )
    {
        var athleteIdValue = user.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(athleteIdValue) || !long.TryParse(athleteIdValue, out var athleteId))
        {
            return null;
        }

        var stored = await tokenStore.GetAsync(athleteId, cancellationToken);
        if (stored is null)
        {
            return null;
        }

        return new GetMeResponse(
            stored.AthleteId,
            stored.FirstName,
            stored.LastName,
            stored.Profile,
            stored.Scope,
            stored.ExpiresAt
        );
    }
}
