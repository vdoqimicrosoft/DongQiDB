using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DongQiDB.Domain.Entities;

namespace DongQiDB.Infrastructure.Data.Configurations;

/// <summary>
/// AI Session entity configuration
/// </summary>
public class AiSessionConfiguration : IEntityTypeConfiguration<AiSession>
{
    public void Configure(EntityTypeBuilder<AiSession> builder)
    {
        builder.ToTable("AiSessions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .HasMaxLength(500);

        builder.Property(e => e.DatabaseType)
            .HasMaxLength(50);

        builder.Property(e => e.LastUserMessage)
            .HasMaxLength(2000);

        builder.Property(e => e.LastAiResponse)
            .HasMaxLength(4000);

        builder.HasOne(e => e.Connection)
            .WithMany()
            .HasForeignKey(e => e.ConnectionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Messages)
            .WithOne(m => m.Session)
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ConnectionId);
        builder.HasIndex(e => e.LastActivityAt);
    }
}
