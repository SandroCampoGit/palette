using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PulseArtists.Data;
using PulseArtists.Models;
using PulseArtists.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Railway: bind to the injected PORT ---------------------------------
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// --- Upload limits (fix: phone photos are 4–8 MB) ------------------------
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = 25_000_000);
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 20_000_000);

// --- Connection string ----------------------------------------------------
var connectionString = BuildNpgsqlConnectionString(builder.Configuration);
builder.Services.AddDbContext<ApplicationDbContext>(opt => opt.UseNpgsql(connectionString));

// --- Identity --------------------------------------------------------------
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(o =>
    {
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequiredLength = 8;
        o.SignIn.RequireConfirmedAccount = false;
        o.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.LogoutPath = "/Account/Logout";
    o.AccessDeniedPath = "/Account/Login";
    o.ExpireTimeSpan = TimeSpan.FromDays(14);
    o.SlidingExpiration = true;
});

// --- Data protection: persist login keys so deploys don't log everyone out
var keyPath = builder.Configuration["DataProtection:KeyPath"];
if (!string.IsNullOrWhiteSpace(keyPath))
{
    Directory.CreateDirectory(keyPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
        .SetApplicationName("PulseArtists");
}

builder.Services.AddScoped<IImageStorage, LocalImageStorage>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// --- Auto-apply migrations on startup --------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

var uploadPath = app.Configuration["Storage:UploadPath"];
var webPrefix = app.Configuration["Storage:WebPrefix"] ?? "/uploads";
if (!string.IsNullOrWhiteSpace(uploadPath))
{
    Directory.CreateDirectory(uploadPath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.GetFullPath(uploadPath)),
        RequestPath = webPrefix
    });
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static string BuildNpgsqlConnectionString(IConfiguration config)
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (string.IsNullOrWhiteSpace(databaseUrl))
        return config.GetConnectionString("DefaultConnection")
               ?? throw new InvalidOperationException("No DATABASE_URL or DefaultConnection configured.");

    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);

    var b = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
        Database = uri.AbsolutePath.TrimStart('/'),
        SslMode = Npgsql.SslMode.Prefer,
        TrustServerCertificate = true
    };
    return b.ConnectionString;
}
