using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Portal.Application.Infrastructure.Persistence;

/// <summary>
/// Used only by EF Core tooling (dotnet ef migrations add / update).
/// Not referenced at runtime.
/// </summary>
public class PortalDbContextFactory : IDesignTimeDbContextFactory<PortalDbContext>
{
    public PortalDbContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var apiDirectory = Directory.Exists(Path.Combine(currentDirectory, "Portal.Api"))
            ? Path.Combine(currentDirectory, "Portal.Api")
            : Path.GetFullPath(Path.Combine(currentDirectory, "..", "Portal.Api"));

        var config = new ConfigurationBuilder()
            .SetBasePath(apiDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<PortalDbContext>();
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=CustomerFirstPortal;Integrated Security=True;TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(connectionString, sql =>
            sql.MigrationsAssembly(typeof(PortalDbContext).Assembly.FullName));

        return new PortalDbContext(optionsBuilder.Options);
    }
}
