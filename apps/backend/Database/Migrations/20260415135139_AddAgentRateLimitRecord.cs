using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseAgentOs.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentRateLimitRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentRateLimits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    BucketKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WindowStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentRateLimits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRateLimits_AgentId",
                table: "AgentRateLimits",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRateLimits_AgentId_BucketKey_WindowStart",
                table: "AgentRateLimits",
                columns: new[] { "AgentId", "BucketKey", "WindowStart" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentRateLimits");
        }
    }
}
