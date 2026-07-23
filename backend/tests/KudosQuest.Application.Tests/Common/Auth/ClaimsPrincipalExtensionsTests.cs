using System.Security.Claims;
using FluentAssertions;
using KudosQuest.Application.Common.Auth;

namespace KudosQuest.Application.Tests.Common.Auth;

public sealed class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void TryGetAthleteId_WhenSubIsValid_ReturnsTrue()
    {
        var user = CreateUser(("sub", "12345"));

        var ok = user.TryGetAthleteId(out var athleteId);

        ok.Should().BeTrue();
        athleteId.Should().Be(12345);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    public void TryGetAthleteId_WhenSubInvalid_ReturnsFalse(string? sub)
    {
        var claims = sub is null ? Array.Empty<Claim>() : [new Claim("sub", sub)];
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var ok = user.TryGetAthleteId(out var athleteId);

        ok.Should().BeFalse();
        athleteId.Should().Be(0);
    }

    private static ClaimsPrincipal CreateUser(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value))));
}
