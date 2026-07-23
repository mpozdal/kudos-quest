namespace KudosQuest.Application.Common.Persistence.Entities;

public sealed class StravaCredential
{
    public long AthleteId { get; set; }

    // NOTE: AccessToken and RefreshToken are stored in plaintext.
    // For production, consider encrypting these at-rest using Data Protection API
    // or database-level encryption (e.g., Postgres pgcrypto, Always Encrypted).
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
    public string Scope { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }

    public Athlete Athlete { get; set; } = null!;
}
