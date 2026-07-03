using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace PulseArtists.Services;

public interface IImageStorage
{
    Task<string> SaveAsync(IFormFile file, CancellationToken ct = default);
    void Delete(string webPath);
}

/// <summary>
/// Accepts uploads up to 15 MB, resizes to max 1600px on the long edge and
/// re-encodes as JPEG (~quality 82). Phone photos come in at 4–8 MB and leave
/// at a few hundred KB, so the Railway volume stays small and pages stay fast.
/// </summary>
public class LocalImageStorage : IImageStorage
{
    private readonly string _root;
    private readonly string _webPrefix;
    private readonly ILogger<LocalImageStorage> _log;

    private static readonly string[] Allowed = { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".heic", ".bmp" };
    public const long MaxBytes = 15 * 1024 * 1024; // 15 MB in
    private const int MaxEdge = 1600;

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
        if (file.Length > MaxBytes) throw new InvalidOperationException("Image exceeds the 15 MB limit.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.Contains(ext)) throw new InvalidOperationException("Unsupported image type.");

        var name = $"{Guid.NewGuid():N}.jpg";
        var full = Path.Combine(_root, name);

        try
        {
            await using var input = file.OpenReadStream();
            using var image = await Image.LoadAsync(input, ct); // decodes jpg/png/webp/gif/bmp

            if (image.Width > MaxEdge || image.Height > MaxEdge)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxEdge, MaxEdge)
                }));
            }

            await image.SaveAsync(full, new JpegEncoder { Quality = 82 }, ct);
            return $"{_webPrefix}/{name}";
        }
        catch (UnknownImageFormatException)
        {
            throw new InvalidOperationException("That file isn't a readable image (iPhone HEIC? Export/share it as JPEG first).");
        }
    }

    public void Delete(string webPath)
    {
        try
        {
            var full = Path.Combine(_root, Path.GetFileName(webPath));
            if (File.Exists(full)) File.Delete(full);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to delete image {Path}", webPath);
        }
    }
}
