namespace KudosQuest.Application.Common.Persistence.Entities;

public sealed class StravaCredential
{
    public long AthleteId { get; set; }

    /// <summary>
    /// Encrypted at rest via <see cref="Common.Auth.ISensitiveDataProtector"/>.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted at rest via <see cref="Common.Auth.ISensitiveDataProtector"/>.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
    public string Scope { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }

    public Athlete Athlete { get; set; } = null!;
}
