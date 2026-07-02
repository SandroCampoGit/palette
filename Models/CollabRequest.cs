using System.ComponentModel.DataAnnotations;

namespace PulseArtists.Models;

public class CollabRequest
{
    public int Id { get; set; }

    // Who sent it
    [Required]
    public string FromUserId { get; set; } = string.Empty;
    public ApplicationUser? FromUser { get; set; }

    // Target artist profile
    public int ToArtistProfileId { get; set; }
    public ArtistProfile? ToArtistProfile { get; set; }

    [Required, MaxLength(1500)]
    public string Message { get; set; } = string.Empty;

    public CollabStatus Status { get; set; } = CollabStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
