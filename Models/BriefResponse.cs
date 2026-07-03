using System.ComponentModel.DataAnnotations;

namespace PulseArtists.Models;

public class BriefResponse
{
    public int Id { get; set; }

    public int BriefId { get; set; }
    public Brief? Brief { get; set; }

    public int ArtistProfileId { get; set; }
    public ArtistProfile? ArtistProfile { get; set; }

    [Required, MaxLength(1500)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
