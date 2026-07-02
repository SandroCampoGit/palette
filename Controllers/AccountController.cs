using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PulseArtists.Models;
using PulseArtists.ViewModels;

namespace PulseArtists.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;

    public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
    {
        _users = users;
        _signIn = signIn;
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = new ApplicationUser
        {
            UserName = vm.Email,
            Email = vm.Email,
            DisplayName = vm.DisplayName,
            PrimaryMode = vm.PrimaryMode,
            City = vm.City,
            Suburb = vm.Suburb
        };

        var result = await _users.CreateAsync(user, vm.Password);
        if (result.Succeeded)
        {
            await _signIn.SignInAsync(user, isPersistent: true);
            // Artists head straight to building their free profile.
            return vm.PrimaryMode == PrimaryMode.Artist
                ? RedirectToAction("Edit", "Profile")
                : RedirectToAction("Index", "Discover");
        }

        foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
        return View(vm);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _signIn.PasswordSignInAsync(
            vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);
            return RedirectToAction("Index", "Discover");
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
