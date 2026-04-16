using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DongQiDB.Domain.Entities;

namespace DongQiDB.Infrastructure.Data.Configurations;

public class IndexInfoConfiguration : IEntityTypeConfiguration<IndexInfo>
{
    public void Configure(EntityTypeBuilder<IndexInfo> builder)
    {
        builder.ToTable("index_info");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id");

        builder.Property(i => i.TableId)
            .HasColumnName("table_id")
            .IsRequired();

        builder.Property(i => i.IndexName)
            .HasColumnName("index_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(i => i.IsUnique)
            .HasColumnName("is_unique")
            .HasDefaultValue(false);

        builder.Property(i => i.IsPrimaryKey)
            .HasColumnName("is_primary_key")
            .HasDefaultValue(false);

        builder.Property(i => i.IndexType)
            .HasColumnName("index_type")
            .HasMaxLength(50);

        builder.Property(i => i.FilterCondition)
            .HasColumnName("filter_condition")
            .HasMaxLength(500);

        builder.Property(i => i.Columns)
            .HasColumnName("columns")
            .HasMaxLength(1000);

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(i => new { i.TableId, i.IndexName }).IsUnique();

        builder.HasOne(i => i.Table)
            .WithMany(t => t.Indexes)
            .HasForeignKey(i => i.TableId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
