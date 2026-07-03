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
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IImageStorage _images;

    public ProfileController(ApplicationDbContext db, UserManager<ApplicationUser> users, IImageStorage images)
    {
        _db = db;
        _users = users;
        _images = images;
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var userId = _users.GetUserId(User)!;
        var profile = await _db.ArtistProfiles
            .Include(p => p.Portfolio.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.UserId == userId);

        var user = await _users.GetUserAsync(User);

        var vm = profile is null
            ? new ProfileEditViewModel
            {
                ArtistName = user?.DisplayName ?? "",
                City = user?.City,
                Suburb = user?.Suburb,
                Latitude = user?.Latitude,
                Longitude = user?.Longitude
            }
            : new ProfileEditViewModel
            {
                Id = profile.Id,
                ArtistName = profile.ArtistName,
                Discipline = profile.Discipline,
                Tagline = profile.Tagline,
                Bio = profile.Bio,
                Website = profile.Website,
                Instagram = profile.Instagram,
                SoundCloud = profile.SoundCloud,
                YouTube = profile.YouTube,
                OpenToCollab = profile.OpenToCollab,
                IsPublished = profile.IsPublished,
                Latitude = user?.Latitude,
                Longitude = user?.Longitude,
                City = user?.City,
                Suburb = user?.Suburb,
                Slug = profile.Slug,
                Portfolio = profile.Portfolio
            };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await ReloadPortfolio(vm);
            return View(vm);
        }

        var user = (await _users.GetUserAsync(User))!;
        var profile = await _db.ArtistProfiles
            .Include(p => p.Portfolio)
            .FirstOrDefaultAsync(p => p.UserId == user.Id);

        // Persist location on the user record (GPS w/ city fallback)
        user.Latitude = vm.Latitude;
        user.Longitude = vm.Longitude;
        user.City = vm.City;
        user.Suburb = vm.Suburb;
        await _users.UpdateAsync(user);

        if (profile is null)
        {
            profile = new ArtistProfile
            {
                UserId = user.Id,
                Slug = await UniqueSlug(vm.ArtistName)
            };
            _db.ArtistProfiles.Add(profile);
        }

        profile.ArtistName = vm.ArtistName;
        profile.Discipline = vm.Discipline;
        profile.Tagline = vm.Tagline;
        profile.Bio = vm.Bio;
        profile.Website = vm.Website;
        profile.Instagram = vm.Instagram;
        profile.SoundCloud = vm.SoundCloud;
        profile.YouTube = vm.YouTube;
        profile.OpenToCollab = vm.OpenToCollab;
        profile.IsPublished = vm.IsPublished;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["Saved"] = "Profile saved.";
        return RedirectToAction(nameof(Edit));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 20_000_000)]
    public async Task<IActionResult> UploadImage(IFormFile? image, string? caption)
    {
        var userId = _users.GetUserId(User)!;
        var profile = await _db.ArtistProfiles
            .Include(p => p.Portfolio)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile is null)
        {
            TempData["Error"] = "Save your profile before adding images.";
            return RedirectToAction(nameof(Edit));
        }

        if (image is null || image.Length == 0)
        {
            TempData["Error"] = "Choose an image first.";
            return RedirectToAction(nameof(Edit));
        }

        try
        {
            var url = await _images.SaveAsync(image);
            profile.Portfolio.Add(new PortfolioImage
            {
                Url = url,
                Caption = caption,
                SortOrder = profile.Portfolio.Count
            });
            await _db.SaveChangesAsync();
            TempData["Saved"] = "Image added.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Edit));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var userId = _users.GetUserId(User)!;
        var img = await _db.PortfolioImages
            .Include(i => i.ArtistProfile)
            .FirstOrDefaultAsync(i => i.Id == id && i.ArtistProfile!.UserId == userId);

        if (img is not null)
        {
            _images.Delete(img.Url);
            _db.PortfolioImages.Remove(img);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Edit));
    }

    private async Task ReloadPortfolio(ProfileEditViewModel vm)
    {
        var userId = _users.GetUserId(User)!;
        var profile = await _db.ArtistProfiles
            .Include(p => p.Portfolio.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.UserId == userId);
        vm.Portfolio = profile?.Portfolio ?? new List<PortfolioImage>();
        vm.Slug = profile?.Slug;
    }

    private async Task<string> UniqueSlug(string name)
    {
        var baseSlug = GeoHelper.Slugify(name);
        var slug = baseSlug;
        var n = 1;
        while (await _db.ArtistProfiles.AnyAsync(p => p.Slug == slug))
            slug = $"{baseSlug}-{++n}";
        return slug;
    }
}
