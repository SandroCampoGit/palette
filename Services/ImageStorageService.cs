namespace PulseArtists.Services;

public interface IImageStorage
{
    Task<string> SaveAsync(IFormFile file, CancellationToken ct = default);
    void Delete(string webPath);
}

/// <summary>
/// Saves images to a physical folder served under /uploads.
/// On Railway the container filesystem is ephemeral, so mount a Volume at the
/// upload path (see README) or swap this class for an S3/Cloudflare R2 impl.
/// </summary>
public class LocalImageStorage : IImageStorage
{
    private readonly string _root;      // absolute physical folder
    private readonly string _webPrefix; // e.g. /uploads
    private readonly ILogger<LocalImageStorage> _log;

    private static readonly string[] Allowed = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    public LocalImageStorage(IWebHostEnvironment env, IConfiguration config, ILogger<LocalImageStorage> log)
    {
        _log = log;
        _webPrefix = config["Storage:WebPrefix"] ?? "/uploads";
        var configured = config["Storage:UploadPath"];
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads")
            : configured;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(IFormFile file, CancellationToken ct = default)
    {
        if (file.Length == 0) throw new InvalidOperationException("Empty file.");
        if (file.Length > MaxBytes) throw new InvalidOperationException("File exceeds 5 MB limit.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.Contains(ext)) throw new InvalidOperationException("Unsupported image type.");

        var name = $"{Guid.NewGuid():N}{ext}";
        var full = Path.Combine(_root, name);
        await using (var stream = File.Create(full))
        {
            await file.CopyToAsync(stream, ct);
        }
        return $"{_webPrefix}/{name}";
    }

    public void Delete(string webPath)
    {
        try
        {
            var name = Path.GetFileName(webPath);
            var full = Path.Combine(_root, name);
            if (File.Exists(full)) File.Delete(full);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to delete image {Path}", webPath);
        }
    }
}
