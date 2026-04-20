using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseAgentOs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OAuthTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EncryptedAccessToken = table.Column<string>(type: "character varying(16384)", maxLength: 16384, nullable: true),
                    EncryptedRefreshToken = table.Column<string>(type: "character varying(16384)", maxLength: 16384, nullable: true),
                    Scopes = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OAuthTokens_Provider",
                table: "OAuthTokens",
                column: "Provider",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OAuthTokens");
        }
    }
}
