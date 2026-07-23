namespace KudosQuest.Application.Common.Auth;

public interface IStravaTokenStore
{
    Task SaveAsync(StoredStravaTokens tokens, CancellationToken cancellationToken = default);
    Task<StoredStravaTokens?> GetAsync(long athleteId, CancellationToken cancellationToken = default);
}
