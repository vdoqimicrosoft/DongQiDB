using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DongQiDB.Infrastructure.Data;

/// <summary>
/// Design-time factory for EF Core migrations
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SystemDbContext>
{
    public SystemDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SystemDbContext>();

        // Default to SQLite for design-time operations
        var connectionString = args.Length > 0 ? args[0] : "Data Source=dongqidb.db";
        optionsBuilder.UseSqlite(connectionString);

        return new SystemDbContext(optionsBuilder.Options);
    }
}
