using System.Text.Json.Serialization;

namespace KudosQuest.Application.Common.Strava;

public sealed class StravaTokenResponse
{
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("athlete")]
    public StravaAthleteSummary? Athlete { get; set; }
}

public sealed class StravaAthleteSummary
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("firstname")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastname")]
    public string? LastName { get; set; }

    [JsonPropertyName("profile")]
    public string? Profile { get; set; }
}
