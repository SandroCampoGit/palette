using System.ComponentModel.DataAnnotations;

namespace PulseArtists.Models;

/// <summary>
/// Trust signal earned through work: only the sender of an ACCEPTED collab
/// request can endorse the artist. One endorsement per collab.
/// </summary>
public class Endorsement
{
    public int Id { get; set; }

    public int CollabRequestId { get; set; }
    public CollabRequest? CollabRequest { get; set; }

    [Required]
    public string FromUserId { get; set; } = string.Empty;
    public ApplicationUser? FromUser { get; set; }

    public int ToArtistProfileId { get; set; }
    public ArtistProfile? ToArtistProfile { get; set; }

    [Required, MaxLength(500)]
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
