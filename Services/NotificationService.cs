using PulseArtists.Data;
using PulseArtists.Models;

namespace PulseArtists.Services;

public interface INotificationService
{
    Task NotifyAsync(string userId, string title, string? body = null, string? url = null);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    public NotificationService(ApplicationDbContext db) => _db = db;

    public async Task NotifyAsync(string userId, string title, string? body = null, string? url = null)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Body = body,
            Url = url
        });
        await _db.SaveChangesAsync();
    }
}
