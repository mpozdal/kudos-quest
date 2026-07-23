using System.Security.Claims;
using FluentAssertions;
using KudosQuest.Application.Common.Auth;
using KudosQuest.Application.Features.Athletes.GetMe;
using NSubstitute;

namespace KudosQuest.Application.Tests.Features.Athletes;

public sealed class GetMeHandlerTests
{
    private readonly IStravaTokenStore _tokenStore = Substitute.For<IStravaTokenStore>();
    private readonly GetMeHandler _sut;

    public GetMeHandlerTests()
    {
        _sut = new GetMeHandler(_tokenStore);
    }

    [Fact]
    public async Task HandleAsync_WhenSubMissing_ReturnsNull()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        (await _sut.HandleAsync(user)).Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenAthleteMissing_ReturnsNull()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "10")]));
        _tokenStore.GetAsync(10, Arg.Any<CancellationToken>()).Returns((StoredStravaTokens?)null);

        (await _sut.HandleAsync(user)).Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenFound_ReturnsResponse()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "10")]));
        _tokenStore
            .GetAsync(10, Arg.Any<CancellationToken>())
            .Returns(
                new StoredStravaTokens
                {
                    AthleteId = 10,
                    AccessToken = "a",
                    RefreshToken = "r",
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                    Scope = "read",
                    FirstName = "Ada",
                    LastName = "Lovelace",
                    Profile = "p",
                }
            );

        var result = await _sut.HandleAsync(user);

        result.Should().NotBeNull();
        result!.AthleteId.Should().Be(10);
        result.FirstName.Should().Be("Ada");
        result.Scope.Should().Be("read");
    }
}
