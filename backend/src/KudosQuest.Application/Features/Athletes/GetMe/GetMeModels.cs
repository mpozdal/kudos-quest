namespace KudosQuest.Application.Features.Athletes.GetMe;

public sealed record GetMeResponse(
    long AthleteId,
    string? FirstName,
    string? LastName,
    string? Profile,
    string Scope,
    DateTimeOffset StravaTokenExpiresAt
);
