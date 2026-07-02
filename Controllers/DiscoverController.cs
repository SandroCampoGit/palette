using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseArtists.Data;
using PulseArtists.Models;
using PulseArtists.Services;
using PulseArtists.ViewModels;

namespace PulseArtists.Controllers;

public class DiscoverController : Controller
{
    private readonly ApplicationDbContext _db;

    public DiscoverController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(
        Discipline? discipline, double? lat, double? lng, string? city, int radiusKm = 50)
    {
        var query = _db.ArtistProfiles
            .Include(p => p.User)
            .Include(p => p.Portfolio.OrderBy(i => i.SortOrder))
            .Where(p => p.IsPublished);

        if (discipline.HasValue)
            query = query.Where(p => p.Discipline == discipline.Value);

        // City fallback filter (applied in SQL when we have no coords)
        if (!lat.HasValue && !string.IsNullOrWhiteSpace(city))
            query = query.Where(p => p.User!.City != null && p.User.City.ToLower() == city.ToLower());

        var rows = await query.ToListAsync();

        var cards = rows.Select(p => new ArtistCard
        {
            Id = p.Id,
            Slug = p.Slug,
            ArtistName = p.ArtistName,
            Discipline = p.Discipline,
            Tagline = p.Tagline,
            CoverImageUrl = p.Portfolio.FirstOrDefault()?.Url,
            City = p.User?.City,
            Latitude = p.User?.Latitude,
            Longitude = p.User?.Longitude,
            OpenToCollab = p.OpenToCollab,
            DistanceKm = (lat.HasValue && lng.HasValue && p.User?.Latitude != null && p.User?.Longitude != null)
                ? Math.Round(GeoHelper.DistanceKm(lat.Value, lng.Value, p.User.Latitude!.Value, p.User.Longitude!.Value), 1)
                : (double?)null
        }).ToList();

        // If we have the finder's location, rank by distance and apply radius.
        if (lat.HasValue && lng.HasValue)
        {
            cards = cards
                .Where(c => c.DistanceKm == null || c.DistanceKm <= radiusKm)
                .OrderBy(c => c.DistanceKm ?? double.MaxValue)
                .ToList();
        }
        else
        {
            cards = cards.OrderByDescending(c => c.Id).ToList();
        }

        var vm = new DiscoverViewModel
        {
            Artists = cards,
            DisciplineFilter = discipline,
            Lat = lat,
            Lng = lng,
            City = city,
            RadiusKm = radiusKm,
            UsingLocation = lat.HasValue && lng.HasValue
        };
        return View(vm);
    }
}
