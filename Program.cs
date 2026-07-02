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

// --- Connection string --------------------------------------------------
// Railway injects DATABASE_URL as a URI (postgresql://user:pass@host:port/db).
// Locally we fall back to appsettings "DefaultConnection".
var connectionString = BuildNpgsqlConnectionString(builder.Configuration);

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseNpgsql(connectionString));

// --- Identity -----------------------------------------------------------
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

builder.Services.AddScoped<IImageStorage, LocalImageStorage>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// --- Auto-apply migrations on startup (handy for Railway deploys) -------
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

// Serve uploads from a configurable (possibly volume-mounted) path.
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


// -----------------------------------------------------------------------
// Converts Railway's DATABASE_URL (URI form) into an Npgsql connection
// string. Falls back to the "DefaultConnection" setting for local dev.
static string BuildNpgsqlConnectionString(IConfiguration config)
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (string.IsNullOrWhiteSpace(databaseUrl))
        return config.GetConnectionString("DefaultConnection")
               ?? throw new InvalidOperationException("No DATABASE_URL or DefaultConnection configured.");

    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var db = uri.AbsolutePath.TrimStart('/');

    var b = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
        Database = db,
        SslMode = Npgsql.SslMode.Prefer,
        TrustServerCertificate = true
    };
    return b.ConnectionString;
}
