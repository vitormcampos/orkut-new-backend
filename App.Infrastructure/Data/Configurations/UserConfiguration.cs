using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id)
            .HasName("pk_users");

        builder.Property(u => u.Id)
            .HasColumnType("uuid")
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(300)
            .HasColumnName("email");

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("ix_users_email");

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("username");

        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("ix_users_username");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("password_hash");

        builder.Property(u => u.ProfilePicture)
            .HasMaxLength(1000)
            .HasColumnName("profile_picture");

        builder.Property(u => u.Bio)
            .HasMaxLength(500)
            .HasColumnName("bio");

        builder.Property(u => u.BirthDate)
            .HasColumnType("date")
            .HasColumnName("birth_date");

        builder.Property(u => u.City)
            .HasMaxLength(100)
            .HasColumnName("city");

        builder.Property(u => u.State)
            .HasMaxLength(100)
            .HasColumnName("state");

        builder.Property(u => u.RelationshipStatus)
            .HasMaxLength(30)
            .HasColumnName("relationship_status")
            .HasConversion<string>();

        builder.Property(u => u.MusicInterests)
            .HasColumnType("text[]")
            .HasColumnName("music_interests");

        builder.Property(u => u.MovieInterests)
            .HasColumnType("text[]")
            .HasColumnName("movie_interests");

        builder.Property(u => u.BookInterests)
            .HasColumnType("text[]")
            .HasColumnName("book_interests");

        builder.Property(u => u.Hobbies)
            .HasColumnType("text[]")
            .HasColumnName("hobbies");

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasColumnName("is_active");

        builder.Property(u => u.DeletedAt)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("deleted_at");

        builder.Property(u => u.LastLoginAt)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("last_login_at");

        builder.Property(u => u.PasswordResetToken)
            .HasMaxLength(500)
            .HasColumnName("password_reset_token");

        builder.Property(u => u.PasswordResetExpiry)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("password_reset_expiry");

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");
    }
}
