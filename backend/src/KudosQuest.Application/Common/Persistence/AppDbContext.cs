using KudosQuest.Application.Common.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace KudosQuest.Application.Common.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Athlete> Athletes => Set<Athlete>();
    public DbSet<StravaCredential> StravaCredentials => Set<StravaCredential>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Athlete>(entity =>
        {
            entity.ToTable("athletes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FirstName).HasMaxLength(200);
            entity.Property(x => x.LastName).HasMaxLength(200);
            entity.Property(x => x.Profile).HasMaxLength(500);
        });

        modelBuilder.Entity<StravaCredential>(entity =>
        {
            entity.ToTable("strava_credentials");
            entity.HasKey(x => x.AthleteId);
            entity.Property(x => x.AccessToken).IsRequired();
            entity.Property(x => x.RefreshToken).IsRequired();
            entity.Property(x => x.Scope).HasMaxLength(500);

            entity
                .HasOne(x => x.Athlete)
                .WithOne(x => x.StravaCredential)
                .HasForeignKey<StravaCredential>(x => x.AthleteId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
