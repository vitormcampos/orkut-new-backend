using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Data.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("posts");
        builder.HasKey(p => p.Id).HasName("pk_posts");
        builder.Property(p => p.Id).HasColumnType("uuid").HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.AuthorId).IsRequired().HasColumnType("uuid").HasColumnName("author_id");
        builder.Property(p => p.CommunityId).HasColumnType("uuid").HasColumnName("community_id");
        builder.Property(p => p.Content).IsRequired().HasMaxLength(Post.MaxContentLength).HasColumnName("content");
        builder.Property(p => p.CreatedAt).IsRequired().HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").HasColumnName("created_at");

        builder.HasIndex(p => new { p.AuthorId, p.CommunityId, p.CreatedAt }).HasDatabaseName("ix_posts_author_community_created_at");
        builder.HasIndex(p => new { p.CommunityId, p.CreatedAt }).HasDatabaseName("ix_posts_community_created_at");

        builder.HasOne(p => p.Author).WithMany().HasForeignKey(p => p.AuthorId)
            .HasConstraintName("fk_posts_author_user_id").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Community).WithMany().HasForeignKey(p => p.CommunityId)
            .HasConstraintName("fk_posts_community_id").OnDelete(DeleteBehavior.Cascade);
    }
}
