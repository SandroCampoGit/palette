using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseArtists.Data;

namespace PulseArtists.Controllers;

[Route("a")]
public class ArtistsController : Controller
{
    private readonly ApplicationDbContext _db;

    public ArtistsController(ApplicationDbContext db) => _db = db;

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var profile = await _db.ArtistProfiles
            .Include(p => p.User)
            .Include(p => p.Portfolio.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

        if (profile is null) return NotFound();

        ViewBag.Endorsements = await _db.Endorsements
            .Include(e => e.FromUser)
            .Where(e => e.ToArtistProfileId == profile.Id)
            .OrderByDescending(e => e.CreatedAtUtc)
            .ToListAsync();

        return View(profile);
    }
}
