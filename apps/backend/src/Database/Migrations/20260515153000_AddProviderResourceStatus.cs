using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OffceOs.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderResourceStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Account",
                table: "ProviderResources",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "ProviderResources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastValidatedAt",
                table: "ProviderResources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phase",
                table: "ProviderResources",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Ready");

            migrationBuilder.AddColumn<string>(
                name: "StatusMessage",
                table: "ProviderResources",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Account",
                table: "ProviderResources");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "ProviderResources");

            migrationBuilder.DropColumn(
                name: "LastValidatedAt",
                table: "ProviderResources");

            migrationBuilder.DropColumn(
                name: "Phase",
                table: "ProviderResources");

            migrationBuilder.DropColumn(
                name: "StatusMessage",
                table: "ProviderResources");
        }
    }
}
