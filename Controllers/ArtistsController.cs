using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseArtists.Data;

namespace PulseArtists.Controllers;

[Route("a")]
public class ArtistsController : Controller
{
    private readonly ApplicationDbContext _db;

    public ArtistsController(ApplicationDbContext db) => _db = db;

    // Public "free promo" page: /a/{slug}
    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var profile = await _db.ArtistProfiles
            .Include(p => p.User)
            .Include(p => p.Portfolio.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

        if (profile is null) return NotFound();
        return View(profile);
    }
}
