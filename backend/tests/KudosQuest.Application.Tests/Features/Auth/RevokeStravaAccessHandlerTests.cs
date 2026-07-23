using System.Security.Claims;
using FluentAssertions;
using KudosQuest.Application.Common.Auth;
using KudosQuest.Application.Common.Strava;
using KudosQuest.Application.Features.Auth.RevokeStravaAccess;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace KudosQuest.Application.Tests.Features.Auth;

public sealed class RevokeStravaAccessHandlerTests
{
    private readonly IStravaTokenStore _tokenStore = Substitute.For<IStravaTokenStore>();
    private readonly IStravaOAuthClient _oauthClient = Substitute.For<IStravaOAuthClient>();
    private readonly ILogger<RevokeStravaAccessHandler> _logger =
        Substitute.For<ILogger<RevokeStravaAccessHandler>>();
    private readonly RevokeStravaAccessHandler _sut;

    public RevokeStravaAccessHandlerTests()
    {
        _sut = new RevokeStravaAccessHandler(_tokenStore, _oauthClient, _logger);
    }

    [Fact]
    public async Task HandleAsync_WhenSubMissing_ThrowsUnauthorized()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var act = () => _sut.HandleAsync(user);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _tokenStore.DidNotReceiveWithAnyArgs().DeleteAsync(default);
    }

    [Fact]
    public async Task HandleAsync_WhenNoCredentials_DoesNothing()
    {
        var user = CreateUser(10);
        _tokenStore.GetAsync(10, Arg.Any<CancellationToken>()).Returns((StoredStravaTokens?)null);

        await _sut.HandleAsync(user);

        await _oauthClient.DidNotReceiveWithAnyArgs().RevokeAsync(default!);
        await _tokenStore.DidNotReceiveWithAnyArgs().DeleteAsync(default);
    }

    [Fact]
    public async Task HandleAsync_WhenCredentialsExist_RevokesAndDeletes()
    {
        var user = CreateUser(10);
        var stored = CreateStored(10);
        _tokenStore.GetAsync(10, Arg.Any<CancellationToken>()).Returns(stored);

        await _sut.HandleAsync(user);

        await _oauthClient
            .Received(1)
            .RevokeAsync(stored.RefreshToken, "refresh_token", Arg.Any<CancellationToken>());
        await _tokenStore.Received(1).DeleteAsync(10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenRevokeFails_StillDeletesLocally()
    {
        var user = CreateUser(10);
        var stored = CreateStored(10);
        _tokenStore.GetAsync(10, Arg.Any<CancellationToken>()).Returns(stored);
        _oauthClient
            .RevokeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Strava down"));

        await _sut.HandleAsync(user);

        await _tokenStore.Received(1).DeleteAsync(10, Arg.Any<CancellationToken>());
    }

    private static ClaimsPrincipal CreateUser(long athleteId) =>
        new(new ClaimsIdentity([new Claim("sub", athleteId.ToString())]));

    private static StoredStravaTokens CreateStored(long athleteId) =>
        new()
        {
            AthleteId = athleteId,
            AccessToken = "access",
            RefreshToken = "refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            Scope = "read",
        };
}
