using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseArtists.Data;
using PulseArtists.Models;
using PulseArtists.Services;

namespace PulseArtists.Controllers;

public class BriefsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IEmailSender _email;

    public BriefsController(ApplicationDbContext db, UserManager<ApplicationUser> users, IEmailSender email)
    {
        _db = db; _users = users; _email = email;
    }

    // Public feed of open opportunities
    [HttpGet]
    public async Task<IActionResult> Index(Discipline? discipline, string? city)
    {
        var q = _db.Briefs
            .Include(b => b.PostedBy)
            .Include(b => b.Responses)
            .Where(b => b.Status == BriefStatus.Open);

        if (discipline.HasValue) q = q.Where(b => b.Discipline == discipline.Value);
        if (!string.IsNullOrWhiteSpace(city))
            q = q.Where(b => b.City != null && b.City.ToLower().Contains(city.ToLower()));

        var briefs = await q.OrderByDescending(b => b.CreatedAtUtc).ToListAsync();
        ViewBag.Discipline = discipline;
        ViewBag.City = city;
        return View(briefs);
    }

    [Authorize]
    [HttpGet]
    public IActionResult Create() => View(new Brief());

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Brief vm)
    {
        ModelState.Remove(nameof(Brief.PostedByUserId));
        if (!ModelState.IsValid) return View(vm);

        var user = (await _users.GetUserAsync(User))!;
        vm.PostedByUserId = user.Id;
        vm.City ??= user.City;
        if (vm.NeededBy.HasValue)
            vm.NeededBy = DateTime.SpecifyKind(vm.NeededBy.Value, DateTimeKind.Utc);

        _db.Briefs.Add(vm);
        await _db.SaveChangesAsync();

        // Notify matching local artists (same discipline; same city when both known).
        var matches = await _db.ArtistProfiles
            .Include(p => p.User)
            .Where(p => p.IsPublished && p.OpenToCollab && p.Discipline == vm.Discipline
                        && p.UserId != user.Id)
            .ToListAsync();

        var briefUrl = Url.Action("Details", "Briefs", new { id = vm.Id }, HttpContext.Request.Scheme);
        foreach (var artist in matches.Where(a =>
                     string.IsNullOrWhiteSpace(vm.City) ||
                     string.Equals(a.User?.City, vm.City, StringComparison.OrdinalIgnoreCase)))
        {
            if (string.IsNullOrEmpty(artist.User?.Email)) continue;
            await _email.SendAsync(artist.User.Email!,
                $"New {vm.Discipline} opportunity near you",
                $"<p>Hi {artist.ArtistName},</p>" +
                $"<p>A new brief matching your discipline was posted on Palette:</p>" +
                $"<p><strong>{System.Net.WebUtility.HtmlEncode(vm.Title)}</strong>" +
                (string.IsNullOrEmpty(vm.Budget) ? "" : $" · {System.Net.WebUtility.HtmlEncode(vm.Budget)}") +
                $"</p><p><a href=\"{briefUrl}\">View the brief and respond</a></p>");
        }

        TempData["Saved"] = "Brief posted. Matching local artists have been notified.";
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var brief = await _db.Briefs
            .Include(b => b.PostedBy)
            .Include(b => b.Responses).ThenInclude(r => r.ArtistProfile)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (brief is null) return NotFound();

        var userId = _users.GetUserId(User);
        ViewBag.IsOwner = userId != null && brief.PostedByUserId == userId;
        ViewBag.MyProfileId = userId == null ? null :
            (await _db.ArtistProfiles.Where(p => p.UserId == userId).Select(p => (int?)p.Id).FirstOrDefaultAsync());

        return View(brief);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Respond(int briefId, string message)
    {
        var userId = _users.GetUserId(User)!;
        var profile = await _db.ArtistProfiles.FirstOrDefaultAsync(p => p.UserId == userId && p.IsPublished);
        if (profile is null)
        {
            TempData["Error"] = "Publish an artist profile before responding to briefs.";
            return RedirectToAction(nameof(Details), new { id = briefId });
        }

        var brief = await _db.Briefs.Include(b => b.PostedBy)
            .FirstOrDefaultAsync(b => b.Id == briefId && b.Status == BriefStatus.Open);
        if (brief is null) return NotFound();

        if (brief.PostedByUserId == userId)
        {
            TempData["Error"] = "That's your own brief.";
            return RedirectToAction(nameof(Details), new { id = briefId });
        }

        if (await _db.BriefResponses.AnyAsync(r => r.BriefId == briefId && r.ArtistProfileId == profile.Id))
        {
            TempData["Error"] = "You've already responded to this brief.";
            return RedirectToAction(nameof(Details), new { id = briefId });
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Add a short pitch.";
            return RedirectToAction(nameof(Details), new { id = briefId });
        }

        _db.BriefResponses.Add(new BriefResponse
        {
            BriefId = briefId,
            ArtistProfileId = profile.Id,
            Message = message.Trim()
        });
        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(brief.PostedBy?.Email))
        {
            var url = Url.Action("Details", "Briefs", new { id = briefId }, HttpContext.Request.Scheme);
            await _email.SendAsync(brief.PostedBy.Email!,
                "An artist responded to your brief",
                $"<p><strong>{profile.ArtistName}</strong> responded to \"{System.Net.WebUtility.HtmlEncode(brief.Title)}\":</p>" +
                $"<blockquote>{System.Net.WebUtility.HtmlEncode(message)}</blockquote>" +
                $"<p><a href=\"{url}\">View responses</a></p>");
        }

        TempData["Saved"] = "Response sent to the poster.";
        return RedirectToAction(nameof(Details), new { id = briefId });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var userId = _users.GetUserId(User)!;
        var brief = await _db.Briefs.FirstOrDefaultAsync(b => b.Id == id && b.PostedByUserId == userId);
        if (brief is not null)
        {
            brief.Status = BriefStatus.Closed;
            await _db.SaveChangesAsync();
            TempData["Saved"] = "Brief closed.";
        }
        return RedirectToAction(nameof(Index));
    }
}
