using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseAgentOs.Database.Migrations
{
    /// <inheritdoc />
    public partial class ScopeIntegrationsByUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OAuthTokens_Provider",
                table: "OAuthTokens");

            migrationBuilder.DropIndex(
                name: "IX_Integrations_Name",
                table: "Integrations");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationCredentials_IntegrationName",
                table: "IntegrationCredentials");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "OAuthTokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Integrations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "IntegrationCredentials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("""
                DELETE FROM "OAuthGrantedScopes"
                WHERE "OAuthTokenId" IN (
                    SELECT "Id" FROM "OAuthTokens"
                    WHERE "UserId" = '00000000-0000-0000-0000-000000000000'
                );
                """);

            migrationBuilder.Sql("""
                DELETE FROM "OAuthTokens"
                WHERE "UserId" = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.Sql("""
                DELETE FROM "IntegrationCredentials"
                WHERE "OwnerId" = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.Sql("""
                DELETE FROM "AgentIntegrations"
                WHERE "IntegrationName" IN (
                    SELECT "Name" FROM "Integrations"
                    WHERE "OwnerId" IS NULL
                );
                """);

            migrationBuilder.Sql("""
                DELETE FROM "Integrations"
                WHERE "OwnerId" IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_OAuthTokens_UserId",
                table: "OAuthTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthTokens_UserId_Provider",
                table: "OAuthTokens",
                columns: new[] { "UserId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_OwnerId",
                table: "Integrations",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_OwnerId_Name",
                table: "Integrations",
                columns: new[] { "OwnerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationCredentials_OwnerId",
                table: "IntegrationCredentials",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationCredentials_OwnerId_IntegrationName",
                table: "IntegrationCredentials",
                columns: new[] { "OwnerId", "IntegrationName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_IntegrationCredentials_Users_OwnerId",
                table: "IntegrationCredentials",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OAuthTokens_Users_UserId",
                table: "OAuthTokens",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IntegrationCredentials_Users_OwnerId",
                table: "IntegrationCredentials");

            migrationBuilder.DropForeignKey(
                name: "FK_OAuthTokens_Users_UserId",
                table: "OAuthTokens");

            migrationBuilder.DropIndex(
                name: "IX_OAuthTokens_UserId",
                table: "OAuthTokens");

            migrationBuilder.DropIndex(
                name: "IX_OAuthTokens_UserId_Provider",
                table: "OAuthTokens");

            migrationBuilder.DropIndex(
                name: "IX_Integrations_OwnerId",
                table: "Integrations");

            migrationBuilder.DropIndex(
                name: "IX_Integrations_OwnerId_Name",
                table: "Integrations");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationCredentials_OwnerId",
                table: "IntegrationCredentials");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationCredentials_OwnerId_IntegrationName",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "OAuthTokens");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "IntegrationCredentials");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthTokens_Provider",
                table: "OAuthTokens",
                column: "Provider",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_Name",
                table: "Integrations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationCredentials_IntegrationName",
                table: "IntegrationCredentials",
                column: "IntegrationName",
                unique: true);
        }
    }
}
