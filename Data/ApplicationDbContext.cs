using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PulseArtists.Models;

namespace PulseArtists.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<ArtistProfile> ArtistProfiles => Set<ArtistProfile>();
    public DbSet<PortfolioImage> PortfolioImages => Set<PortfolioImage>();
    public DbSet<CollabRequest> CollabRequests => Set<CollabRequest>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<ArtistProfile>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.User)
             .WithOne(u => u.ArtistProfile)
             .HasForeignKey<ArtistProfile>(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PortfolioImage>(e =>
        {
            e.HasOne(x => x.ArtistProfile)
             .WithMany(p => p.Portfolio)
             .HasForeignKey(x => x.ArtistProfileId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CollabRequest>(e =>
        {
            e.HasOne(x => x.FromUser)
             .WithMany()
             .HasForeignKey(x => x.FromUserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ToArtistProfile)
             .WithMany()
             .HasForeignKey(x => x.ToArtistProfileId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.ToArtistProfileId, x.Status });
        });
    }
}
