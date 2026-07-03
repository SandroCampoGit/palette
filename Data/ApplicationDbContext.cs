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
    public DbSet<Brief> Briefs => Set<Brief>();
    public DbSet<BriefResponse> BriefResponses => Set<BriefResponse>();
    public DbSet<Endorsement> Endorsements => Set<Endorsement>();

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
            e.HasOne(x => x.FromUser).WithMany()
             .HasForeignKey(x => x.FromUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ToArtistProfile).WithMany()
             .HasForeignKey(x => x.ToArtistProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ToArtistProfileId, x.Status });
        });

        b.Entity<Brief>(e =>
        {
            e.HasOne(x => x.PostedBy).WithMany()
             .HasForeignKey(x => x.PostedByUserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.Status, x.Discipline });
        });

        b.Entity<BriefResponse>(e =>
        {
            e.HasOne(x => x.Brief).WithMany(br => br.Responses)
             .HasForeignKey(x => x.BriefId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ArtistProfile).WithMany()
             .HasForeignKey(x => x.ArtistProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.BriefId, x.ArtistProfileId }).IsUnique(); // one pitch per artist per brief
        });

        b.Entity<Endorsement>(e =>
        {
            e.HasIndex(x => x.CollabRequestId).IsUnique(); // one endorsement per collab
            e.HasOne(x => x.CollabRequest).WithMany()
             .HasForeignKey(x => x.CollabRequestId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.FromUser).WithMany()
             .HasForeignKey(x => x.FromUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ToArtistProfile).WithMany()
             .HasForeignKey(x => x.ToArtistProfileId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
