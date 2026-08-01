using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Broccoli.Avalonia.Storage;

/// <summary>
/// Enables `dotnet ef migrations add ...` to construct a <see cref="BroccoliDbContext"/>
/// at design time (there is no DI container/host to resolve it from at the moment).
/// Runtime code should construct <see cref="BroccoliDbContext"/> via <see cref="AppPaths.DatabaseFilePath"/>
/// the same way.
/// </summary>
public class BroccoliDbContextFactory : IDesignTimeDbContextFactory<BroccoliDbContext>
{
    public BroccoliDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BroccoliDbContext>()
            .UseSqlite($"Data Source={AppPaths.DatabaseFilePath}")
            .Options;

        return new BroccoliDbContext(options);
    }
}
