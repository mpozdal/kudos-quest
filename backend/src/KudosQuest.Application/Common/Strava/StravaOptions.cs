namespace KudosQuest.Application.Common.Strava;

public sealed class StravaOptions
{
    public const string SectionName = "Strava";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string AuthorizeUrl { get; set; } = "https://www.strava.com/oauth/authorize";
    public string TokenUrl { get; set; } = "https://www.strava.com/oauth/token";
    public string Scopes { get; set; } = "read,activity:read_all,profile:read_all";
}
