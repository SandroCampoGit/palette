using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseArtists.Data;
using PulseArtists.Models;

namespace PulseArtists.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public NotificationsController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db; _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _users.GetUserId(User)!;
        var items = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(100)
            .ToListAsync();

        // Mark unread as read now that they've opened the page.
        var unread = items.Where(n => !n.IsRead).ToList();
        if (unread.Count > 0)
        {
            unread.ForEach(n => n.IsRead = true);
            await _db.SaveChangesAsync();
        }

        return View(items);
    }
}
