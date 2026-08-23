using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Data.Configurations;

public class CommunityMemberConfiguration : IEntityTypeConfiguration<CommunityMember>
{
    public void Configure(EntityTypeBuilder<CommunityMember> builder)
    {
        builder.ToTable("community_members");

        builder.HasKey(m => m.Id)
            .HasName("pk_community_members");

        builder.Property(m => m.Id)
            .HasColumnType("uuid")
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(m => m.CommunityId)
            .IsRequired()
            .HasColumnType("uuid")
            .HasColumnName("community_id");

        builder.Property(m => m.UserId)
            .IsRequired()
            .HasColumnType("uuid")
            .HasColumnName("user_id");

        builder.Property(m => m.Role)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("role")
            .HasConversion<string>();

        builder.Property(m => m.JoinedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .HasColumnName("joined_at");

        builder.HasIndex(m => new { m.CommunityId, m.UserId })
            .IsUnique()
            .HasDatabaseName("ix_community_members_community_user");

        builder.HasIndex(m => m.UserId)
            .HasDatabaseName("ix_community_members_user_id");

        builder.HasOne(m => m.Community)
            .WithMany(c => c.Members)
            .HasForeignKey(m => m.CommunityId)
            .HasConstraintName("fk_community_members_community_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .HasConstraintName("fk_community_members_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
