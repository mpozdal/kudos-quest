using System.Security.Claims;

namespace KudosQuest.Application.Common.Auth;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetAthleteId(this ClaimsPrincipal user, out long athleteId)
    {
        athleteId = default;
        var athleteIdValue = user.FindFirstValue("sub");
        return !string.IsNullOrWhiteSpace(athleteIdValue)
            && long.TryParse(athleteIdValue, out athleteId);
    }
}
