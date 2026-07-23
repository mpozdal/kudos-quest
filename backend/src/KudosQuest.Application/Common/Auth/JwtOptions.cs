namespace KudosQuest.Application.Common.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "kudosquest";
    public string Audience { get; set; } = "kudosquest";
    public string Key { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 10080;
}
