using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using KudosQuest.Application.Common.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KudosQuest.Application.Tests.Common.Auth;

public sealed class JwtTokenServiceTests
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "kudosquest-test",
        Audience = "kudosquest-test",
        Key = "unit-test-signing-key-at-least-32-chars!!",
        ExpirationMinutes = 60,
    };

    [Fact]
    public void CreateToken_ContainsSubAndNameClaims()
    {
        var sut = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(Options));
        var athlete = new AuthenticatedAthlete(42, "Jan", "Kowalski");

        var token = sut.CreateToken(athlete);

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var jwt = handler.ReadJwtToken(token);

        jwt.Subject.Should().Be("42");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.GivenName && c.Value == "Jan");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.FamilyName && c.Value == "Kowalski");
    }

    [Fact]
    public void CreateToken_CanBeValidatedWithMatchingParameters()
    {
        var sut = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(Options));
        var token = sut.CreateToken(new AuthenticatedAthlete(7, null, null));

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(
            token,
            JwtTokenService.CreateValidationParameters(Options),
            out _
        );

        principal.FindFirstValue("sub").Should().Be("7");
    }
}
