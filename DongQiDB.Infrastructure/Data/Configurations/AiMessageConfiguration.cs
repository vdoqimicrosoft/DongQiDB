using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DongQiDB.Domain.Entities;

namespace DongQiDB.Infrastructure.Data.Configurations;

/// <summary>
/// AI Message entity configuration
/// </summary>
public class AiMessageConfiguration : IEntityTypeConfiguration<AiMessage>
{
    public void Configure(EntityTypeBuilder<AiMessage> builder)
    {
        builder.ToTable("AiMessages");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Role)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Content)
            .HasMaxLength(16000); // ~4k tokens worth of text

        builder.Property(e => e.SqlGenerated)
            .HasMaxLength(8000);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000);

        builder.HasIndex(e => e.SessionId);
        builder.HasIndex(e => e.CreatedAt);
    }
}
