using KudosQuest.Application.Common.Auth;
using KudosQuest.Application.Common.Persistence;
using KudosQuest.Application.Common.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace KudosQuest.Application.Common.Persistence;

public sealed class EfStravaTokenStore(AppDbContext db) : IStravaTokenStore
{
    public async Task SaveAsync(StoredStravaTokens tokens, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var athlete = await db.Athletes.FirstOrDefaultAsync(x => x.Id == tokens.AthleteId, cancellationToken);

        if (athlete is null)
        {
            athlete = new Athlete
            {
                Id = tokens.AthleteId,
                FirstName = tokens.FirstName,
                LastName = tokens.LastName,
                Profile = tokens.Profile,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.Athletes.Add(athlete);
        }
        else
        {
            athlete.FirstName = tokens.FirstName;
            athlete.LastName = tokens.LastName;
            athlete.Profile = tokens.Profile;
            athlete.UpdatedAt = now;
        }

        var credential = await db.StravaCredentials.FirstOrDefaultAsync(
            x => x.AthleteId == tokens.AthleteId,
            cancellationToken
        );

        if (credential is null)
        {
            db.StravaCredentials.Add(
                new StravaCredential
                {
                    AthleteId = tokens.AthleteId,
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken,
                    ExpiresAt = tokens.ExpiresAt,
                    Scope = tokens.Scope,
                    UpdatedAt = now,
                }
            );
        }
        else
        {
            credential.AccessToken = tokens.AccessToken;
            credential.RefreshToken = tokens.RefreshToken;
            credential.ExpiresAt = tokens.ExpiresAt;
            credential.Scope = tokens.Scope;
            credential.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<StoredStravaTokens?> GetAsync(
        long athleteId,
        CancellationToken cancellationToken = default
    )
    {
        var row = await db
            .Athletes.AsNoTracking()
            .Where(x => x.Id == athleteId)
            .Select(x => new
            {
                x.Id,
                x.FirstName,
                x.LastName,
                x.Profile,
                Credential = x.StravaCredential,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row?.Credential is null)
        {
            return null;
        }

        return new StoredStravaTokens
        {
            AthleteId = row.Id,
            AccessToken = row.Credential.AccessToken,
            RefreshToken = row.Credential.RefreshToken,
            ExpiresAt = row.Credential.ExpiresAt,
            Scope = row.Credential.Scope,
            FirstName = row.FirstName,
            LastName = row.LastName,
            Profile = row.Profile,
        };
    }
}
