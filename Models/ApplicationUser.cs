using Microsoft.AspNetCore.Identity;

namespace PulseArtists.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public PrimaryMode PrimaryMode { get; set; } = PrimaryMode.Finder;

    // Location – GPS with city fallback
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? City { get; set; }
    public string? Suburb { get; set; }
    public string Country { get; set; } = "South Africa";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public ArtistProfile? ArtistProfile { get; set; }
}
