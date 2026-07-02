using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseArtists.Data;
using PulseArtists.Models;
using PulseArtists.ViewModels;

namespace PulseArtists.Controllers;

[Authorize]
public class CollabController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public CollabController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Request(int artistProfileId, string message, string? returnSlug)
    {
        var userId = _users.GetUserId(User)!;
        var target = await _db.ArtistProfiles.FirstOrDefaultAsync(p => p.Id == artistProfileId && p.IsPublished);

        if (target is null)
        {
            TempData["Error"] = "That artist isn't available.";
            return RedirectToAction("Index", "Discover");
        }

        if (target.UserId == userId)
        {
            TempData["Error"] = "You can't send a collab request to yourself.";
            return returnSlug != null
                ? RedirectToAction("Details", "Artists", new { slug = returnSlug })
                : RedirectToAction("Index", "Discover");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Add a short message.";
            return returnSlug != null
                ? RedirectToAction("Details", "Artists", new { slug = returnSlug })
                : RedirectToAction("Index", "Discover");
        }

        _db.CollabRequests.Add(new CollabRequest
        {
            FromUserId = userId,
            ToArtistProfileId = target.Id,
            Message = message.Trim()
        });
        await _db.SaveChangesAsync();

        TempData["Saved"] = "Collab request sent.";
        return returnSlug != null
            ? RedirectToAction("Details", "Artists", new { slug = returnSlug })
            : RedirectToAction(nameof(Inbox));
    }

    [HttpGet]
    public async Task<IActionResult> Inbox()
    {
        var userId = _users.GetUserId(User)!;
        var myProfile = await _db.ArtistProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        var received = myProfile is null
            ? new List<CollabRequest>()
            : await _db.CollabRequests
                .Include(r => r.FromUser)
                .Where(r => r.ToArtistProfileId == myProfile.Id)
                .OrderByDescending(r => r.CreatedAtUtc)
                .ToListAsync();

        var sent = await _db.CollabRequests
            .Include(r => r.ToArtistProfile)
            .Where(r => r.FromUserId == userId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();

        return View(new CollabInboxViewModel
        {
            Received = received,
            Sent = sent,
            HasArtistProfile = myProfile is not null
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Respond(int id, bool accept)
    {
        var userId = _users.GetUserId(User)!;
        var req = await _db.CollabRequests
            .Include(r => r.ToArtistProfile)
            .FirstOrDefaultAsync(r => r.Id == id && r.ToArtistProfile!.UserId == userId);

        if (req is not null)
        {
            req.Status = accept ? CollabStatus.Accepted : CollabStatus.Declined;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Inbox));
    }
}
