using FluentAssertions;
using KudosQuest.Application.Common.Auth;
using KudosQuest.Application.Common.Strava;
using KudosQuest.Application.Features.Auth.HandleStravaCallback;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace KudosQuest.Application.Tests.Features.Auth;

public sealed class HandleStravaCallbackHandlerTests
{
    private readonly IOAuthStateStore _stateStore = Substitute.For<IOAuthStateStore>();
    private readonly IStravaOAuthClient _oauthClient = Substitute.For<IStravaOAuthClient>();
    private readonly IStravaTokenStore _tokenStore = Substitute.For<IStravaTokenStore>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly HandleStravaCallbackHandler _sut;

    public HandleStravaCallbackHandlerTests()
    {
        var jwtOptions = Options.Create(
            new JwtOptions
            {
                Issuer = "test",
                Audience = "test",
                Key = "unit-test-signing-key-at-least-32-chars!!",
                ExpirationMinutes = 60,
            }
        );

        _sut = new HandleStravaCallbackHandler(
            _stateStore,
            _oauthClient,
            _tokenStore,
            _jwtTokenService,
            jwtOptions
        );
    }

    [Fact]
    public async Task HandleAsync_WhenErrorPresent_Throws()
    {
        var act = () =>
            _sut.HandleAsync(new HandleStravaCallbackCommand(null, null, null, "access_denied"));

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*access_denied*");
    }

    [Fact]
    public async Task HandleAsync_WhenStateInvalid_Throws()
    {
        _stateStore.TryConsume("bad").Returns(false);

        var act = () =>
            _sut.HandleAsync(new HandleStravaCallbackCommand("code", "read", "bad", null));

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*state*");
    }

    [Fact]
    public async Task HandleAsync_WhenValid_SavesTokensAndReturnsJwt()
    {
        _stateStore.TryConsume("state").Returns(true);
        _oauthClient
            .ExchangeCodeAsync("code", Arg.Any<CancellationToken>())
            .Returns(
                new StravaTokenResponse
                {
                    AccessToken = "strava-access",
                    RefreshToken = "strava-refresh",
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(6).ToUnixTimeSeconds(),
                    Athlete = new StravaAthleteSummary
                    {
                        Id = 55,
                        FirstName = "Ada",
                        LastName = "Lovelace",
                        Profile = "https://example.com/a.png",
                    },
                }
            );
        _jwtTokenService
            .CreateToken(Arg.Any<AuthenticatedAthlete>())
            .Returns("jwt-token");

        var result = await _sut.HandleAsync(
            new HandleStravaCallbackCommand("code", "read,activity:read_all", "state", null)
        );

        result.AccessToken.Should().Be("jwt-token");
        result.AthleteId.Should().Be(55);
        result.TokenType.Should().Be("Bearer");
        await _tokenStore
            .Received(1)
            .SaveAsync(
                Arg.Is<StoredStravaTokens>(t =>
                    t!.AthleteId == 55 && t.AccessToken == "strava-access"
                ),
                Arg.Any<CancellationToken>()
            );
    }
}
