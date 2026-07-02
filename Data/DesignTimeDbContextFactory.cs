using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PulseArtists.Data;

/// <summary>
/// Used only by the EF Core CLI (migrations / database update) so it doesn't
/// have to run the full app startup. Reads DATABASE_URL/DefaultConnection if set,
/// otherwise uses a local default. Never used at runtime.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("DATABASE_CONNECTION")
                 ?? "Host=localhost;Port=5432;Database=palette;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new ApplicationDbContext(options);
    }
}
