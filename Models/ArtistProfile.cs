using System.ComponentModel.DataAnnotations;

namespace PulseArtists.Models;

public class ArtistProfile
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    // Public shareable slug: /a/{slug}  -> the "free promo" link an artist shares
    [Required, MaxLength(80)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string ArtistName { get; set; } = string.Empty;

    public Discipline Discipline { get; set; } = Discipline.Other;

    [MaxLength(1200)]
    public string? Bio { get; set; }

    [MaxLength(200)]
    public string? Tagline { get; set; }

    // Contact / links (all optional)
    [MaxLength(200)] public string? Website { get; set; }
    [MaxLength(120)] public string? Instagram { get; set; }
    [MaxLength(120)] public string? SoundCloud { get; set; }
    [MaxLength(120)] public string? YouTube { get; set; }

    public bool OpenToCollab { get; set; } = true;
    public bool IsPublished { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<PortfolioImage> Portfolio { get; set; } = new();
}
