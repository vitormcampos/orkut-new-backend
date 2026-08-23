using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Data.Configurations;

public class CommunityConfiguration : IEntityTypeConfiguration<Community>
{
    public void Configure(EntityTypeBuilder<Community> builder)
    {
        builder.ToTable("communities");

        builder.HasKey(c => c.Id)
            .HasName("pk_communities");

        builder.Property(c => c.Id)
            .HasColumnType("uuid")
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.Property(c => c.Description)
            .HasMaxLength(2000)
            .HasColumnName("description");

        builder.Property(c => c.Photo)
            .HasMaxLength(1000)
            .HasColumnName("photo");

        builder.Property(c => c.OwnerId)
            .IsRequired()
            .HasColumnType("uuid")
            .HasColumnName("owner_id");

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(c => c.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("updated_at");

        builder.HasIndex(c => c.OwnerId)
            .HasDatabaseName("ix_communities_owner_id");

        builder.HasOne(c => c.Owner)
            .WithMany()
            .HasForeignKey(c => c.OwnerId)
            .HasConstraintName("fk_communities_owner_user_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
