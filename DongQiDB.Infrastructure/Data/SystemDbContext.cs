using Microsoft.EntityFrameworkCore;
using DongQiDB.Domain.Entities;

namespace DongQiDB.Infrastructure.Data;

/// <summary>
/// System database context for managing DongQiDB metadata
/// </summary>
public class SystemDbContext : DbContext
{
    public SystemDbContext(DbContextOptions<SystemDbContext> options) : base(options)
    {
    }

    public DbSet<Connection> Connections => Set<Connection>();
    public DbSet<TableInfo> Tables => Set<TableInfo>();
    public DbSet<ColumnInfo> Columns => Set<ColumnInfo>();
    public DbSet<IndexInfo> Indexes => Set<IndexInfo>();
    public DbSet<QueryHistory> QueryHistories => Set<QueryHistory>();
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();
    public DbSet<AiSession> AiSessions => Set<AiSession>();
    public DbSet<AiMessage> AiMessages => Set<AiMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SystemDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Domain.Common.BaseEntity baseEntity)
            {
                baseEntity.UpdatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                {
                    baseEntity.CreatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
