using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DongQiDB.Domain.Entities;

namespace DongQiDB.Infrastructure.Data.Configurations;

public class ColumnInfoConfiguration : IEntityTypeConfiguration<ColumnInfo>
{
    public void Configure(EntityTypeBuilder<ColumnInfo> builder)
    {
        builder.ToTable("column_info");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.TableId)
            .HasColumnName("table_id")
            .IsRequired();

        builder.Property(c => c.ColumnName)
            .HasColumnName("column_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.DataType)
            .HasColumnName("data_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.ColumnType)
            .HasColumnName("column_type")
            .HasMaxLength(100);

        builder.Property(c => c.MaxLength)
            .HasColumnName("max_length");

        builder.Property(c => c.Precision)
            .HasColumnName("precision");

        builder.Property(c => c.Scale)
            .HasColumnName("scale");

        builder.Property(c => c.IsNullable)
            .HasColumnName("is_nullable")
            .HasDefaultValue(true);

        builder.Property(c => c.IsPrimaryKey)
            .HasColumnName("is_primary_key")
            .HasDefaultValue(false);

        builder.Property(c => c.IsForeignKey)
            .HasColumnName("is_foreign_key")
            .HasDefaultValue(false);

        builder.Property(c => c.IsAutoIncrement)
            .HasColumnName("is_auto_increment")
            .HasDefaultValue(false);

        builder.Property(c => c.DefaultValue)
            .HasColumnName("default_value")
            .HasMaxLength(500);

        builder.Property(c => c.ColumnComment)
            .HasColumnName("column_comment")
            .HasMaxLength(1000);

        builder.Property(c => c.OrdinalPosition)
            .HasColumnName("ordinal_position");

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(c => new { c.TableId, c.ColumnName }).IsUnique();
        builder.HasIndex(c => c.IsPrimaryKey);

        builder.HasOne(c => c.Table)
            .WithMany(t => t.Columns)
            .HasForeignKey(c => c.TableId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
