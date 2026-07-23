namespace KudosQuest.Application.Common.Persistence.Entities;

public sealed class Athlete
{
    public long Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Profile { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public StravaCredential? StravaCredential { get; set; }
}
