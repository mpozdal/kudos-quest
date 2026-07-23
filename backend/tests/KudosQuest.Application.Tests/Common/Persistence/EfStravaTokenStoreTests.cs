using FluentAssertions;
using KudosQuest.Application.Common.Auth;
using KudosQuest.Application.Common.Persistence;
using KudosQuest.Application.Common.Persistence.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace KudosQuest.Application.Tests.Common.Persistence;

public sealed class EfStravaTokenStoreTests
{
    [Fact]
    public async Task SaveAsync_ThenGetAsync_RoundTripsDecryptedTokens()
    {
        await using var db = CreateDb();
        var protector = CreateProtector();
        var sut = new EfStravaTokenStore(db, protector);

        await sut.SaveAsync(
            new StoredStravaTokens
            {
                AthleteId = 1,
                AccessToken = "access",
                RefreshToken = "refresh",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                Scope = "read",
                FirstName = "Ada",
                LastName = "Lovelace",
                Profile = "https://example.com/a.png",
            }
        );

        var stored = await sut.GetAsync(1);

        stored.Should().NotBeNull();
        stored!.AccessToken.Should().Be("access");
        stored.RefreshToken.Should().Be("refresh");
        stored.FirstName.Should().Be("Ada");

        var raw = await db.StravaCredentials.AsNoTracking().SingleAsync();
        raw.AccessToken.Should().NotBe("access");
        raw.RefreshToken.Should().NotBe("refresh");
    }

    [Fact]
    public async Task SaveAsync_WhenAthleteExists_UpdatesProfileAndCredentials()
    {
        await using var db = CreateDb();
        var sut = new EfStravaTokenStore(db, CreateProtector());

        await sut.SaveAsync(CreateTokens(1, "a1", "r1", "Ada"));
        await sut.SaveAsync(CreateTokens(1, "a2", "r2", "Augusta"));

        var stored = await sut.GetAsync(1);
        stored!.AccessToken.Should().Be("a2");
        stored.RefreshToken.Should().Be("r2");
        stored.FirstName.Should().Be("Augusta");
        (await db.Athletes.CountAsync()).Should().Be(1);
        (await db.StravaCredentials.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetAsync_WhenMissing_ReturnsNull()
    {
        await using var db = CreateDb();
        var sut = new EfStravaTokenStore(db, CreateProtector());

        (await sut.GetAsync(123)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_RemovesCredentialsOnly()
    {
        await using var db = CreateDb();
        var sut = new EfStravaTokenStore(db, CreateProtector());
        await sut.SaveAsync(CreateTokens(5, "a", "r", "Ada"));

        await sut.DeleteAsync(5);

        (await sut.GetAsync(5)).Should().BeNull();
        (await db.Athletes.CountAsync()).Should().Be(1);
        (await db.StravaCredentials.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_DoesNotThrow()
    {
        await using var db = CreateDb();
        var sut = new EfStravaTokenStore(db, CreateProtector());

        var act = () => sut.DeleteAsync(999);
        await act.Should().NotThrowAsync();
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ISensitiveDataProtector CreateProtector() =>
        new SensitiveDataProtector(DataProtectionProvider.Create(Guid.NewGuid().ToString()));

    private static StoredStravaTokens CreateTokens(
        long id,
        string access,
        string refresh,
        string firstName
    ) =>
        new()
        {
            AthleteId = id,
            AccessToken = access,
            RefreshToken = refresh,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            Scope = "read",
            FirstName = firstName,
            LastName = "Test",
            Profile = null,
        };
}
