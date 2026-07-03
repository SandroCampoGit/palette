using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseArtists.Data;
using PulseArtists.Models;

namespace PulseArtists.ViewComponents;

public class NotificationBadgeViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public NotificationBadgeViewComponent(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db; _users = users;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = _users.GetUserId(HttpContext.User);
        if (string.IsNullOrEmpty(userId)) return View(0);
        var count = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        return View(count);
    }
}
