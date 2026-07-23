namespace KudosQuest.Application.Common.Strava;

public interface IStravaAccessTokenProvider
{
    Task<string?> GetValidTokenAsync(long athleteId, CancellationToken cancellationToken = default);
}