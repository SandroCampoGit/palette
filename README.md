# Palette — find local artists (Pulse Apps)

ASP.NET Core 8 MVC · EF Core (PostgreSQL) · ASP.NET Identity · Leaflet/OpenStreetMap.
Same stack as DataDrop / Imali. A two-sided app: **Creators** get a free shareable
profile and get found; **Finders** discover nearby artists and send collab requests.

## What's in v1
- Register/login with a mode choice (Creator / Finder) — custom `AccountController`, no hidden Identity UI.
- Artist profile: discipline, tagline, bio, links, portfolio images, publish toggle.
- Public shareable page at `/a/{slug}` — this is the "free promo" link.
- Discover: nearby artists ranked by distance (GPS) with city/suburb fallback, discipline filter, Leaflet map.
- Collab requests: send from a profile, accept/decline in your inbox.

## Run locally (Visual Studio)
1. Open `PulseArtists.sln`.
2. Have a local PostgreSQL running, or point `ConnectionStrings:DefaultConnection`
   in `appsettings.json` at your DB.
3. Restore + create the initial schema:
   ```bash
   dotnet tool install --global dotnet-ef      # once, if you don't have it
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
   (The app also auto-applies migrations on startup, so once the migration exists
   it will run itself on first launch.)
4. F5. Browse to `/`.

> The project was authored but **not compiled in the build sandbox** (NuGet was
> blocked there). Do a `dotnet restore` / build in VS first — nothing exotic, just
> the standard restore.

## Deploy to Railway
1. Push to GitHub, create a Railway project from the repo. Railway detects the
   `Dockerfile` and builds it.
2. Add a **PostgreSQL** plugin. Railway injects `DATABASE_URL` automatically —
   `Program.cs` parses that URI into an Npgsql connection string, so no manual config.
3. Railway sets `PORT`; the app binds to it automatically.
4. Migrations apply on boot (`db.Database.Migrate()`), so the schema is created on
   first deploy.

### Image uploads on Railway (important)
Uploaded portfolio images save to disk, and Railway's container filesystem is
**ephemeral** — they'd vanish on redeploy. Two options:
- **Quick:** add a Railway **Volume**, mount it at e.g. `/data/uploads`, and set env
  var `Storage__UploadPath=/data/uploads`. `Program.cs` already serves that path at `/uploads`.
- **Production:** swap `LocalImageStorage` for an S3 / Cloudflare R2 implementation
  of `IImageStorage` (single-class change).

## Rename the brand
The display name "Palette" lives in `Views/Shared/_Layout.cshtml` (`brand` variable).
Namespace stays `PulseArtists`.

## Switch to SQL Server (optional)
Because it's EF Core, swap the provider: replace `UseNpgsql(...)` with
`UseSqlServer(...)` in `Program.cs` + `DesignTimeDbContextFactory`, change the
package, re-add migrations.

## Roadmap (post-v1)
- Object storage for images (R2/S3).
- Messaging thread instead of one-shot collab request.
- Email notifications on new requests (needed to keep artists coming back).
- Profile verification / spam controls before public launch.
