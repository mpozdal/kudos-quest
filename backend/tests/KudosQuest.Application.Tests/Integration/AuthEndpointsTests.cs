using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using KudosQuest.Application.Common.Auth;
using KudosQuest.Application.Common.Persistence;
using KudosQuest.Application.Common.Strava;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace KudosQuest.Application.Tests.Integration;

public sealed class AuthEndpointsTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;

    public AuthEndpointsTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartStravaOAuth_RedirectsToStrava()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        var response = await client.GetAsync("/api/auth/strava");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("strava.com/oauth/authorize");
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithValidJwtAndCredentials_ReturnsAthlete()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IStravaTokenStore>();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        await store.SaveAsync(
            new StoredStravaTokens
            {
                AthleteId = 77,
                AccessToken = "access",
                RefreshToken = "refresh",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                Scope = "read",
                FirstName = "Ada",
                LastName = "Lovelace",
                Profile = null,
            }
        );

        var token = jwt.CreateToken(new AuthenticatedAthlete(77, "Ada", "Lovelace"));
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("athleteId").GetInt64().Should().Be(77);
    }

    [Fact]
    public async Task Callback_WithInvalidState_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(
            "/api/auth/callback?code=x&state=invalid&scope=read"
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_WithValidJwt_ReturnsNoContent()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IStravaTokenStore>();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var oauth = _factory.OAuthClient;

        oauth.ClearReceivedCalls();
        oauth
            .RevokeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await store.SaveAsync(
            new StoredStravaTokens
            {
                AthleteId = 88,
                AccessToken = "access",
                RefreshToken = "refresh",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                Scope = "read",
            }
        );

        var token = jwt.CreateToken(new AuthenticatedAthlete(88, null, null));
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync("/api/auth/logout", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await store.GetAsync(88)).Should().BeNull();
    }

    [Fact]
    public async Task GetMe_WhenAthleteMissing_ReturnsNotFound()
    {
        using var scope = _factory.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var token = jwt.CreateToken(new AuthenticatedAthlete(999, "Ghost", null));
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/me");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Logout_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/auth/logout", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithInvalidSubject_ReturnsUnauthorized()
    {
        using var scope = _factory.Services.CreateScope();
        var jwtOptions = scope.ServiceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: [new Claim("sub", "not-a-number")],
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );
        var bearer = new JwtSecurityTokenHandler().WriteToken(token);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

        var response = await client.PostAsync("/api/auth/logout", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Callback_WithValidState_ReturnsOk()
    {
        using var scope = _factory.Services.CreateScope();
        var stateStore = scope.ServiceProvider.GetRequiredService<IOAuthStateStore>();
        var state = stateStore.Create();

        _factory.OAuthClient.ClearReceivedCalls();
        _factory
            .OAuthClient.ExchangeCodeAsync("auth-code", Arg.Any<CancellationToken>())
            .Returns(
                new StravaTokenResponse
                {
                    AccessToken = "access",
                    RefreshToken = "refresh",
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds(),
                    Athlete = new StravaAthleteSummary
                    {
                        Id = 42,
                        FirstName = "Ada",
                        LastName = "Lovelace",
                    },
                }
            );

        var client = _factory.CreateClient();
        var response = await client.GetAsync(
            $"/api/auth/callback?code=auth-code&state={state}&scope=read"
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("athleteId").GetInt64().Should().Be(42);
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
    }
}

public sealed class AuthWebApplicationFactory : WebApplicationFactory<Program>
{
    public IStravaOAuthClient OAuthClient { get; } = Substitute.For<IStravaOAuthClient>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
            (_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Default"] = "Host=localhost;Database=tests",
                        ["Jwt:Issuer"] = "kudosquest-test",
                        ["Jwt:Audience"] = "kudosquest-test",
                        ["Jwt:Key"] = "unit-test-signing-key-at-least-32-chars!!",
                        ["Jwt:ExpirationMinutes"] = "60",
                        ["Strava:ClientId"] = "123",
                        ["Strava:ClientSecret"] = "secret",
                        ["Strava:RedirectUri"] = "http://localhost/api/auth/callback",
                        ["Strava:Scopes"] = "read",
                    }
                );
            }
        );

        builder.ConfigureTestServices(services =>
        {
            RemoveDbContext(services);

            var dbName = $"KudosQuestTests-{Guid.NewGuid()}";
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));

            services.RemoveAll<IStravaOAuthClient>();
            OAuthClient
                .BuildAuthorizeUrl(Arg.Any<string>())
                .Returns(ci =>
                    $"https://www.strava.com/oauth/authorize?state={ci.Arg<string>()}&client_id=123"
                );
            services.AddSingleton(OAuthClient);
        });
    }

    private static void RemoveDbContext(IServiceCollection services)
    {
        var descriptors = services
            .Where(d =>
                d.ServiceType == typeof(AppDbContext)
                || d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                || (
                    d.ServiceType.IsGenericType
                    && d.ServiceType.GetGenericTypeDefinition().Name.Contains("DbContext")
                )
                || (
                    d.ImplementationType?.FullName?.Contains("EntityFrameworkCore") == true
                    && d.ServiceType.FullName?.Contains("AppDbContext") == true
                )
            )
            .ToList();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }

        // Also remove EF options configuration registered by AddDbContext
        var efOptions = services
            .Where(d =>
                d.ServiceType == typeof(DbContextOptions)
                || (
                    d.ServiceType.IsGenericType
                    && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)
                )
            )
            .ToList();
        foreach (var descriptor in efOptions)
        {
            services.Remove(descriptor);
        }
    }
}
