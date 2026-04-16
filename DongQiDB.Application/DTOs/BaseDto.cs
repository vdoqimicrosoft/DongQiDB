namespace DongQiDB.Application.DTOs;

/// <summary>
/// Base DTO with common properties
/// </summary>
public abstract class BaseDto
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
