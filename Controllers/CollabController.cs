using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseArtists.Data;
using PulseArtists.Models;
using PulseArtists.Services;
using PulseArtists.ViewModels;

namespace PulseArtists.Controllers;

[Authorize]
public class CollabController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IEmailSender _email;

    public CollabController(ApplicationDbContext db, UserManager<ApplicationUser> users, IEmailSender email)
    {
        _db = db; _users = users; _email = email;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Request(int artistProfileId, string message, string? returnSlug)
    {
        var userId = _users.GetUserId(User)!;
        var target = await _db.ArtistProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == artistProfileId && p.IsPublished);

        if (target is null)
        {
            TempData["Error"] = "That artist isn't available.";
            return RedirectToAction("Index", "Discover");
        }

        IActionResult Back() => returnSlug != null
            ? RedirectToAction("Details", "Artists", new { slug = returnSlug })
            : RedirectToAction("Index", "Discover");

        if (target.UserId == userId)
        {
            TempData["Error"] = "You can't send a collab request to yourself.";
            return Back();
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Add a short message.";
            return Back();
        }

        _db.CollabRequests.Add(new CollabRequest
        {
            FromUserId = userId,
            ToArtistProfileId = target.Id,
            Message = message.Trim()
        });
        await _db.SaveChangesAsync();

        // Notify the artist — this is the retention loop.
        var fromUser = await _users.GetUserAsync(User);
        if (!string.IsNullOrEmpty(target.User?.Email))
        {
            var inboxUrl = Url.Action("Inbox", "Collab", null, HttpContext.Request.Scheme);
            var body =
                $"<p>Hi {target.ArtistName},</p>" +
                $"<p><strong>{fromUser?.DisplayName ?? "Someone"}</strong> sent you a collab request on Palette:</p>" +
                $"<blockquote>{System.Net.WebUtility.HtmlEncode(message)}</blockquote>" +
                $"<p><a href=\"{inboxUrl}\">Open your inbox</a></p>";
            await _email.SendAsync(target.User.Email!, "New collab request on Palette", body);
        }

        TempData["Saved"] = "Collab request sent.";
        return returnSlug != null ? Back() : RedirectToAction(nameof(Inbox));
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

        var endorsedIds = await _db.Endorsements
            .Where(e => e.FromUserId == userId)
            .Select(e => e.CollabRequestId)
            .ToListAsync();

        return View(new CollabInboxViewModel
        {
            Received = received,
            Sent = sent,
            HasArtistProfile = myProfile is not null,
            EndorsedCollabIds = endorsedIds
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

    /// <summary>Endorse an artist after an ACCEPTED collab you initiated. One per collab.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Endorse(int collabRequestId, string comment)
    {
        var userId = _users.GetUserId(User)!;
        var collab = await _db.CollabRequests
            .FirstOrDefaultAsync(r => r.Id == collabRequestId
                                   && r.FromUserId == userId
                                   && r.Status == CollabStatus.Accepted);

        if (collab is null)
        {
            TempData["Error"] = "You can only endorse after an accepted collab.";
            return RedirectToAction(nameof(Inbox));
        }

        if (await _db.Endorsements.AnyAsync(e => e.CollabRequestId == collabRequestId))
        {
            TempData["Error"] = "You've already endorsed this collab.";
            return RedirectToAction(nameof(Inbox));
        }

        if (string.IsNullOrWhiteSpace(comment))
        {
            TempData["Error"] = "Write a line about working with them.";
            return RedirectToAction(nameof(Inbox));
        }

        _db.Endorsements.Add(new Endorsement
        {
            CollabRequestId = collab.Id,
            FromUserId = userId,
            ToArtistProfileId = collab.ToArtistProfileId,
            Comment = comment.Trim()
        });
        await _db.SaveChangesAsync();

        TempData["Saved"] = "Endorsement added — it now shows on their public profile.";
        return RedirectToAction(nameof(Inbox));
    }
}
