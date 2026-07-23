namespace KudosQuest.Application.Common.Auth;

public sealed record AuthenticatedAthlete(long AthleteId, string? FirstName, string? LastName);
