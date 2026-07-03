using System.ComponentModel.DataAnnotations;

namespace PulseArtists.Models;

/// <summary>An opportunity posted by a finder (or artist): "I need a muralist in Braamfontein, R5k".</summary>
public class Brief
{
    public int Id { get; set; }

    [Required]
    public string PostedByUserId { get; set; } = string.Empty;
    public ApplicationUser? PostedBy { get; set; }

    [Required, MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public Discipline Discipline { get; set; } = Discipline.Other;

    [MaxLength(80)]
    public string? City { get; set; }

    [MaxLength(60), Display(Name = "Budget (optional)")]
    public string? Budget { get; set; }

    [DataType(DataType.Date), Display(Name = "Needed by (optional)")]
    public DateTime? NeededBy { get; set; }

    public BriefStatus Status { get; set; } = BriefStatus.Open;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<BriefResponse> Responses { get; set; } = new();
}
