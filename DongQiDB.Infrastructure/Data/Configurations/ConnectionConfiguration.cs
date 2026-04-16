using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DongQiDB.Domain.Entities;
using DongQiDB.Domain.Common;

namespace DongQiDB.Infrastructure.Data.Configurations;

public class ConnectionConfiguration : IEntityTypeConfiguration<Connection>
{
    public void Configure(EntityTypeBuilder<Connection> builder)
    {
        builder.ToTable("connections");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Host)
            .HasColumnName("host")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.Port)
            .HasColumnName("port")
            .IsRequired();

        builder.Property(c => c.Database)
            .HasColumnName("database_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Username)
            .HasColumnName("username")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.EncryptedPassword)
            .HasColumnName("encrypted_password")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.DatabaseType)
            .HasColumnName("database_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(c => c.Name).IsUnique();
        builder.HasIndex(c => c.DatabaseType);
    }
}
