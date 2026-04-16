namespace DongQiDB.Domain.Common;

/// <summary>
/// Base entity with common properties
/// </summary>
public abstract class BaseEntity
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
