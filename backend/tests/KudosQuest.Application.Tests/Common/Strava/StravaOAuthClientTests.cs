using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using KudosQuest.Application.Common.Strava;
using Microsoft.Extensions.Options;

namespace KudosQuest.Application.Tests.Common.Strava;

public sealed class StravaOAuthClientTests
{
    [Fact]
    public void BuildAuthorizeUrl_ContainsRequiredQueryParameters()
    {
        var sut = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var url = sut.BuildAuthorizeUrl("abc123");

        url.Should().StartWith("https://www.strava.com/oauth/authorize?");
        url.Should().Contain("client_id=42");
        url.Should().Contain("state=abc123");
        url.Should().Contain("response_type=code");
        url.Should().Contain(Uri.EscapeDataString("http://localhost/callback"));
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenSuccess_ReturnsTokenWithAthlete()
    {
        var payload = JsonSerializer.Serialize(
            new
            {
                token_type = "Bearer",
                access_token = "access",
                refresh_token = "refresh",
                expires_at = 1700000000,
                expires_in = 21600,
                athlete = new
                {
                    id = 9,
                    firstname = "Ada",
                    lastname = "Lovelace",
                    profile = "https://example.com/a.png",
                },
            }
        );

        var sut = CreateSut(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            }
        );

        var result = await sut.ExchangeCodeAsync("code");

        result.AccessToken.Should().Be("access");
        result.Athlete.Should().NotBeNull();
        result.Athlete!.Id.Should().Be(9);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenHttpFails_Throws()
    {
        var sut = CreateSut(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("nope"),
            }
        );

        var act = () => sut.ExchangeCodeAsync("code");
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*token exchange*");
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenAthleteMissing_StillSucceeds()
    {
        var payload = JsonSerializer.Serialize(
            new
            {
                access_token = "access2",
                refresh_token = "refresh2",
                expires_at = 1700000000,
            }
        );
        var sut = CreateSut(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            }
        );

        var result = await sut.RefreshTokenAsync("refresh-old");
        result.AccessToken.Should().Be("access2");
        result.Athlete.Should().BeNull();
    }

    [Fact]
    public async Task RevokeAsync_WhenSuccess_DoesNotThrow()
    {
        HttpRequestMessage? captured = null;
        var sut = CreateSut(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await sut.RevokeAsync("tok", "refresh_token");

        captured.Should().NotBeNull();
        captured!.Method.Should().Be(HttpMethod.Post);
        captured.Headers.Authorization.Should().NotBeNull();
        captured.Headers.Authorization!.Scheme.Should().Be("Basic");
    }

    [Fact]
    public async Task RevokeAsync_WhenHttpFails_Throws()
    {
        var sut = CreateSut(_ =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("denied"),
            }
        );

        var act = () => sut.RevokeAsync("tok");
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*revoke*");
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenBodyEmpty_Throws()
    {
        var sut = CreateSut(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", Encoding.UTF8, "application/json"),
            }
        );

        var act = () => sut.ExchangeCodeAsync("code");
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*empty*");
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenAccessTokenMissing_Throws()
    {
        var payload = JsonSerializer.Serialize(new { refresh_token = "r", athlete = new { id = 1 } });
        var sut = CreateSut(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            }
        );

        var act = () => sut.ExchangeCodeAsync("code");
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*incomplete*");
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenAthleteMissing_Throws()
    {
        var payload = JsonSerializer.Serialize(
            new
            {
                access_token = "a",
                refresh_token = "r",
                expires_at = 1700000000,
            }
        );
        var sut = CreateSut(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            }
        );

        var act = () => sut.ExchangeCodeAsync("code");
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*athlete*");
    }

    private static StravaOAuthClient CreateSut(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new StubHandler(responder);
        var httpClient = new HttpClient(handler);
        var options = Options.Create(
            new StravaOptions
            {
                ClientId = "42",
                ClientSecret = "secret",
                RedirectUri = "http://localhost/callback",
                Scopes = "read,activity:read_all",
            }
        );
        return new StravaOAuthClient(httpClient, options);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(responder(request));
    }
}
