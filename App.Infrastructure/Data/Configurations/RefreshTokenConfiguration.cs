using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(rt => rt.Id)
            .HasName("pk_refresh_tokens");

        builder.Property(rt => rt.Id)
            .HasColumnType("uuid")
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(rt => rt.UserId)
            .IsRequired()
            .HasColumnType("uuid")
            .HasColumnName("user_id");

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("token");

        builder.Property(rt => rt.ExpiresAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasColumnName("expires_at");

        builder.Property(rt => rt.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(rt => rt.IsRevoked)
            .IsRequired()
            .HasColumnName("is_revoked");

        builder.HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("ix_refresh_tokens_token");

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("ix_refresh_tokens_user_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_refresh_tokens_users_user_id");
    }
}
