using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Data.Configurations;

public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.ToTable("friendships");

        builder.HasKey(f => f.Id)
            .HasName("pk_friendships");

        builder.Property(f => f.Id)
            .HasColumnType("uuid")
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(f => f.RequesterId)
            .IsRequired()
            .HasColumnType("uuid")
            .HasColumnName("requester_id");

        builder.Property(f => f.AddresseeId)
            .IsRequired()
            .HasColumnType("uuid")
            .HasColumnName("addressee_id");

        builder.Property(f => f.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("status")
            .HasConversion<string>();

        builder.Property(f => f.RequestedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .HasColumnName("requested_at");

        builder.Property(f => f.RespondedAt)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("responded_at");

        builder.HasIndex(f => new { f.RequesterId, f.AddresseeId })
            .IsUnique()
            .HasDatabaseName("ix_friendships_requester_addressee");

        builder.HasIndex(f => f.AddresseeId)
            .HasDatabaseName("ix_friendships_addressee_id");

        builder.HasIndex(f => f.Status)
            .HasDatabaseName("ix_friendships_status");

        builder.HasOne(f => f.Requester)
            .WithMany()
            .HasForeignKey(f => f.RequesterId)
            .HasConstraintName("fk_friendships_requester_user_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Addressee)
            .WithMany()
            .HasForeignKey(f => f.AddresseeId)
            .HasConstraintName("fk_friendships_addressee_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
