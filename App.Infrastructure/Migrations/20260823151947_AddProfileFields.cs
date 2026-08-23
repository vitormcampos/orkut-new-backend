using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "birth_date",
                table: "users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "book_interests",
                table: "users",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "hobbies",
                table: "users",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "movie_interests",
                table: "users",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "music_interests",
                table: "users",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "relationship_status",
                table: "users",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "state",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "birth_date",
                table: "users");

            migrationBuilder.DropColumn(
                name: "book_interests",
                table: "users");

            migrationBuilder.DropColumn(
                name: "city",
                table: "users");

            migrationBuilder.DropColumn(
                name: "hobbies",
                table: "users");

            migrationBuilder.DropColumn(
                name: "movie_interests",
                table: "users");

            migrationBuilder.DropColumn(
                name: "music_interests",
                table: "users");

            migrationBuilder.DropColumn(
                name: "relationship_status",
                table: "users");

            migrationBuilder.DropColumn(
                name: "state",
                table: "users");
        }
    }
}
