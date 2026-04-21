using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseAgentOs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthGrantedScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scopes",
                table: "OAuthTokens");

            migrationBuilder.CreateTable(
                name: "OAuthGrantedScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OAuthTokenId = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthGrantedScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OAuthGrantedScopes_OAuthTokens_OAuthTokenId",
                        column: x => x.OAuthTokenId,
                        principalTable: "OAuthTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OAuthGrantedScopes_OAuthTokenId_Scope",
                table: "OAuthGrantedScopes",
                columns: new[] { "OAuthTokenId", "Scope" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OAuthGrantedScopes");

            migrationBuilder.AddColumn<string>(
                name: "Scopes",
                table: "OAuthTokens",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: false,
                defaultValue: "");
        }
    }
}
