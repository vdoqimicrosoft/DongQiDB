using DongQiDB.Domain.Common;

namespace DongQiDB.Domain.Entities;

/// <summary>
/// Database connection entity
/// </summary>
public class Connection : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string EncryptedPassword { get; set; } = string.Empty;
    public DatabaseType DatabaseType { get; set; }

    // Navigation properties
    public virtual ICollection<TableInfo> Tables { get; set; } = new List<TableInfo>();
    public virtual ICollection<QueryHistory> QueryHistories { get; set; } = new List<QueryHistory>();
}
