namespace PulseArtists.Models;

public class PortfolioImage
{
    public int Id { get; set; }

    public int ArtistProfileId { get; set; }
    public ArtistProfile? ArtistProfile { get; set; }

    public string Url { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public int SortOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
