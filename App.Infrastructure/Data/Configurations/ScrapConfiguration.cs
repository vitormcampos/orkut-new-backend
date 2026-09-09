using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Data.Configurations;

public class ScrapConfiguration : IEntityTypeConfiguration<Scrap>
{
    public void Configure(EntityTypeBuilder<Scrap> builder)
    {
        builder.ToTable("scraps");
        builder.HasKey(s => s.Id).HasName("pk_scraps");

        builder.Property(s => s.Id)
            .HasColumnType("uuid")
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(s => s.AuthorId).IsRequired().HasColumnType("uuid").HasColumnName("author_id");
        builder.Property(s => s.RecipientId).IsRequired().HasColumnType("uuid").HasColumnName("recipient_id");
        builder.Property(s => s.Content).IsRequired().HasMaxLength(Scrap.MaxContentLength).HasColumnName("content");
        builder.Property(s => s.Visibility)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("visibility")
            .HasConversion<string>();
        builder.Property(s => s.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.HasIndex(s => new { s.RecipientId, s.CreatedAt })
            .HasDatabaseName("ix_scraps_recipient_created_at");
        builder.HasIndex(s => s.AuthorId).HasDatabaseName("ix_scraps_author_id");

        builder.HasOne(s => s.Author)
            .WithMany()
            .HasForeignKey(s => s.AuthorId)
            .HasConstraintName("fk_scraps_author_user_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.Recipient)
            .WithMany()
            .HasForeignKey(s => s.RecipientId)
            .HasConstraintName("fk_scraps_recipient_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
