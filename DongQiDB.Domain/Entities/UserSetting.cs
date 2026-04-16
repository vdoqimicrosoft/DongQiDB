using DongQiDB.Domain.Common;

namespace DongQiDB.Domain.Entities;

/// <summary>
/// User settings entity
/// </summary>
public class UserSetting : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string SettingKey { get; set; } = string.Empty;
    public string? SettingValue { get; set; }
    public string? Description { get; set; }
}
