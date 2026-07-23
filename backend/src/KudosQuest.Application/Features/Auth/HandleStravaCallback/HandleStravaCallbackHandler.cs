using KudosQuest.Application.Common.Auth;
using KudosQuest.Application.Common.Strava;
using Microsoft.Extensions.Options;

namespace KudosQuest.Application.Features.Auth.HandleStravaCallback;

public sealed class HandleStravaCallbackHandler(
    IOAuthStateStore stateStore,
    IStravaOAuthClient stravaOAuthClient,
    IStravaTokenStore tokenStore,
    IJwtTokenService jwtTokenService,
    IOptions<JwtOptions> jwtOptions
)
{
    public async Task<HandleStravaCallbackResponse> HandleAsync(
        HandleStravaCallbackCommand command,
        CancellationToken cancellationToken = default
    )
    {
        if (!string.IsNullOrWhiteSpace(command.Error))
        {
            throw new InvalidOperationException($"Strava authorization denied: {command.Error}");
        }

        if (string.IsNullOrWhiteSpace(command.Code) || string.IsNullOrWhiteSpace(command.State))
        {
            throw new InvalidOperationException("Missing code or state from Strava callback.");
        }

        if (!stateStore.TryConsume(command.State))
        {
            throw new InvalidOperationException("Invalid or expired OAuth state.");
        }

        var token = await stravaOAuthClient.ExchangeCodeAsync(command.Code, cancellationToken);
        var athlete =
            token.Athlete ?? throw new InvalidOperationException("Strava response missing athlete.");

        var stored = new StoredStravaTokens
        {
            AthleteId = athlete.Id,
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(token.ExpiresAt),
            Scope = command.Scope ?? string.Empty,
            FirstName = athlete.FirstName,
            LastName = athlete.LastName,
            Profile = athlete.Profile,
        };

        await tokenStore.SaveAsync(stored, cancellationToken);

        var authenticatedAthlete = new AuthenticatedAthlete(
            athlete.Id,
            athlete.FirstName,
            athlete.LastName
        );
        var accessToken = jwtTokenService.CreateToken(authenticatedAthlete);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.Value.ExpirationMinutes);

        return new HandleStravaCallbackResponse(
            AccessToken: accessToken,
            TokenType: "Bearer",
            AccessTokenExpiresAt: expiresAt,
            AthleteId: athlete.Id,
            FirstName: athlete.FirstName,
            LastName: athlete.LastName,
            Profile: athlete.Profile,
            Scope: stored.Scope,
            StravaTokenExpiresAt: stored.ExpiresAt
        );
    }
}
