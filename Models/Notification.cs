using System.ComponentModel.DataAnnotations;

namespace PulseArtists.Models;

/// <summary>In-app notification (replaces outbound email — no cost, no SMTP).</summary>
public class Notification
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;   // recipient
    public ApplicationUser? User { get; set; }

    [Required, MaxLength(140)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(400)]
    public string? Body { get; set; }

    [MaxLength(300)]
    public string? Url { get; set; }   // relative link to act on

    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
