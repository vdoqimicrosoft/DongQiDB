using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DongQiDB.Domain.Entities;

namespace DongQiDB.Infrastructure.Data.Configurations;

public class QueryHistoryConfiguration : IEntityTypeConfiguration<QueryHistory>
{
    public void Configure(EntityTypeBuilder<QueryHistory> builder)
    {
        builder.ToTable("query_history");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Id)
            .HasColumnName("id");

        builder.Property(q => q.ConnectionId)
            .HasColumnName("connection_id")
            .IsRequired();

        builder.Property(q => q.NaturalLanguageQuery)
            .HasColumnName("natural_language_query")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(q => q.GeneratedSql)
            .HasColumnName("generated_sql")
            .HasMaxLength(8000)
            .IsRequired();

        builder.Property(q => q.IsSuccess)
            .HasColumnName("is_success")
            .IsRequired();

        builder.Property(q => q.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(2000);

        builder.Property(q => q.ExecutionTimeMs)
            .HasColumnName("execution_time_ms");

        builder.Property(q => q.RowCount)
            .HasColumnName("row_count");

        builder.Property(q => q.AiModel)
            .HasColumnName("ai_model")
            .HasMaxLength(100);

        builder.Property(q => q.SessionId)
            .HasColumnName("session_id")
            .HasMaxLength(100);

        builder.Property(q => q.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(q => q.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(q => q.ConnectionId);
        builder.HasIndex(q => q.SessionId);
        builder.HasIndex(q => q.CreatedAt);

        builder.HasOne(q => q.Connection)
            .WithMany(c => c.QueryHistories)
            .HasForeignKey(q => q.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
