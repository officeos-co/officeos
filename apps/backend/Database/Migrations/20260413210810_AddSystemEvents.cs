using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseAgentOs.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    DetailJson = table.Column<string>(type: "text", nullable: true),
                    SkillName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Acknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemEvents_AgentId",
                table: "SystemEvents",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemEvents_Category",
                table: "SystemEvents",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SystemEvents_CreatedAt",
                table: "SystemEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SystemEvents_Severity",
                table: "SystemEvents",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_SystemEvents_SkillName",
                table: "SystemEvents",
                column: "SkillName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemEvents");
        }
    }
}
