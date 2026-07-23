namespace KudosQuest.Application.Common.Auth;

public interface IJwtTokenService
{
    string CreateToken(AuthenticatedAthlete athlete);
}
