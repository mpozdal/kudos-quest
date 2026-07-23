namespace KudosQuest.Application.Features.Auth.HandleStravaCallback;

public sealed record HandleStravaCallbackCommand(string? Code, string? Scope, string? State, string? Error);

public sealed record HandleStravaCallbackResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset AccessTokenExpiresAt,
    long AthleteId,
    string? FirstName,
    string? LastName,
    string? Profile,
    string Scope,
    DateTimeOffset StravaTokenExpiresAt
);
