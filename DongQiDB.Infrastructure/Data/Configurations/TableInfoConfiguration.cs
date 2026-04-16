using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DongQiDB.Domain.Entities;

namespace DongQiDB.Infrastructure.Data.Configurations;

public class TableInfoConfiguration : IEntityTypeConfiguration<TableInfo>
{
    public void Configure(EntityTypeBuilder<TableInfo> builder)
    {
        builder.ToTable("table_info");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id");

        builder.Property(t => t.ConnectionId)
            .HasColumnName("connection_id")
            .IsRequired();

        builder.Property(t => t.SchemaName)
            .HasColumnName("schema_name")
            .HasMaxLength(100);

        builder.Property(t => t.TableName)
            .HasColumnName("table_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.TableComment)
            .HasColumnName("table_comment")
            .HasMaxLength(1000);

        builder.Property(t => t.RowCount)
            .HasColumnName("row_count");

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(t => new { t.ConnectionId, t.SchemaName, t.TableName }).IsUnique();
        builder.HasIndex(t => t.TableName);

        builder.HasOne(t => t.Connection)
            .WithMany(c => c.Tables)
            .HasForeignKey(t => t.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
