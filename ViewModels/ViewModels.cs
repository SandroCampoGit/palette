using System.ComponentModel.DataAnnotations;
using PulseArtists.Models;

namespace PulseArtists.ViewModels;

public class RegisterViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, Display(Name = "Display name"), MaxLength(80)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords don't match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "I'm joining as")]
    public PrimaryMode PrimaryMode { get; set; } = PrimaryMode.Artist;

    public string? City { get; set; }
    public string? Suburb { get; set; }
}

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }
}

public class ProfileEditViewModel
{
    public int? Id { get; set; }

    [Required, Display(Name = "Artist / act name"), MaxLength(80)]
    public string ArtistName { get; set; } = string.Empty;

    public Discipline Discipline { get; set; } = Discipline.Other;

    [MaxLength(200), Display(Name = "One-line tagline")]
    public string? Tagline { get; set; }

    [MaxLength(1200)]
    public string? Bio { get; set; }

    [MaxLength(200)] public string? Website { get; set; }
    [MaxLength(120)] public string? Instagram { get; set; }
    [MaxLength(120)] public string? SoundCloud { get; set; }
    [MaxLength(120)] public string? YouTube { get; set; }

    [Display(Name = "Open to collaboration")]
    public bool OpenToCollab { get; set; } = true;

    [Display(Name = "Publish (make my profile public)")]
    public bool IsPublished { get; set; } = true;

    // Location
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? City { get; set; }
    public string? Suburb { get; set; }

    public string? Slug { get; set; }
    public List<PortfolioImage> Portfolio { get; set; } = new();
}

public class DiscoverViewModel
{
    public List<ArtistCard> Artists { get; set; } = new();
    public Discipline? DisciplineFilter { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public string? City { get; set; }
    public int RadiusKm { get; set; } = 50;
    public bool UsingLocation { get; set; }
}

public class ArtistCard
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public Discipline Discipline { get; set; }
    public string? Tagline { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? DistanceKm { get; set; }
    public bool OpenToCollab { get; set; }
}

public class CollabInboxViewModel
{
    public List<CollabRequest> Received { get; set; } = new();
    public List<CollabRequest> Sent { get; set; } = new();
    public bool HasArtistProfile { get; set; }
}
