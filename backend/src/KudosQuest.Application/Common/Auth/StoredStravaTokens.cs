namespace KudosQuest.Application.Common.Auth;

public sealed class StoredStravaTokens
{
    public required long AthleteId { get; init; }
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required string Scope { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Profile { get; init; }
}
