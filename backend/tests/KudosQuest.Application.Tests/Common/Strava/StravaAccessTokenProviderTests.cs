using FluentAssertions;
using KudosQuest.Application.Common.Auth;
using KudosQuest.Application.Common.Strava;
using NSubstitute;

namespace KudosQuest.Application.Tests.Common.Strava;

public sealed class StravaAccessTokenProviderTests
{
    private readonly IStravaTokenStore _tokenStore = Substitute.For<IStravaTokenStore>();
    private readonly IStravaOAuthClient _oauthClient = Substitute.For<IStravaOAuthClient>();
    private readonly StravaAccessTokenProvider _sut;

    public StravaAccessTokenProviderTests()
    {
        _sut = new StravaAccessTokenProvider(_tokenStore, _oauthClient);
    }

    [Fact]
    public async Task GetValidTokenAsync_WhenNoCredentials_ReturnsNull()
    {
        _tokenStore.GetAsync(1, Arg.Any<CancellationToken>()).Returns((StoredStravaTokens?)null);

        var result = await _sut.GetValidTokenAsync(1);

        result.Should().BeNull();
        await _oauthClient.DidNotReceiveWithAnyArgs().RefreshTokenAsync(default!);
    }

    [Fact]
    public async Task GetValidTokenAsync_WhenTokenStillValid_ReturnsAccessTokenWithoutRefresh()
    {
        var stored = CreateStored(expiresAt: DateTimeOffset.UtcNow.AddHours(2));
        _tokenStore.GetAsync(stored.AthleteId, Arg.Any<CancellationToken>()).Returns(stored);

        var result = await _sut.GetValidTokenAsync(stored.AthleteId);

        result.Should().Be("access-old");
        await _oauthClient.DidNotReceiveWithAnyArgs().RefreshTokenAsync(default!);
        await _tokenStore.DidNotReceiveWithAnyArgs().SaveAsync(default!);
    }

    [Fact]
    public async Task GetValidTokenAsync_WhenExpired_RefreshesAndSaves()
    {
        var stored = CreateStored(expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));
        _tokenStore.GetAsync(stored.AthleteId, Arg.Any<CancellationToken>()).Returns(stored);
        _oauthClient
            .RefreshTokenAsync(stored.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(
                new StravaTokenResponse
                {
                    AccessToken = "access-new",
                    RefreshToken = "refresh-new",
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(6).ToUnixTimeSeconds(),
                }
            );

        var result = await _sut.GetValidTokenAsync(stored.AthleteId);

        result.Should().Be("access-new");
        await _tokenStore
            .Received(1)
            .SaveAsync(
                Arg.Is<StoredStravaTokens>(t =>
                    t!.AccessToken == "access-new" && t.RefreshToken == "refresh-new"
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task GetValidTokenAsync_WhenRefreshOmitsRefreshToken_KeepsExistingRefreshToken()
    {
        var stored = CreateStored(expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));
        _tokenStore.GetAsync(stored.AthleteId, Arg.Any<CancellationToken>()).Returns(stored);
        _oauthClient
            .RefreshTokenAsync(stored.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(
                new StravaTokenResponse
                {
                    AccessToken = "access-new",
                    RefreshToken = "",
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(6).ToUnixTimeSeconds(),
                }
            );

        await _sut.GetValidTokenAsync(stored.AthleteId);

        await _tokenStore
            .Received(1)
            .SaveAsync(
                Arg.Is<StoredStravaTokens>(t => t!.RefreshToken == "refresh-old"),
                Arg.Any<CancellationToken>()
            );
    }

    private static StoredStravaTokens CreateStored(DateTimeOffset expiresAt) =>
        new()
        {
            AthleteId = 99,
            AccessToken = "access-old",
            RefreshToken = "refresh-old",
            ExpiresAt = expiresAt,
            Scope = "read",
            FirstName = "Jan",
            LastName = "Kowalski",
            Profile = null,
        };
}
