using FluentAssertions;
using KudosQuest.Application.Common.Auth;
using Microsoft.AspNetCore.DataProtection;

namespace KudosQuest.Application.Tests.Common.Auth;

public sealed class SensitiveDataProtectorTests
{
    [Fact]
    public void Protect_ThenUnprotect_RoundTrips()
    {
        var provider = DataProtectionProvider.Create("KudosQuest.Tests");
        var sut = new SensitiveDataProtector(provider);

        var protectedPayload = sut.Protect("secret-token");
        protectedPayload.Should().NotBe("secret-token");
        sut.Unprotect(protectedPayload).Should().Be("secret-token");
    }
}
