using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OffceOs.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentRunPurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DefinitionId",
                table: "AgentRuns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "AgentRuns",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_DefinitionId",
                table: "AgentRuns",
                column: "DefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgentRuns_DefinitionId",
                table: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "DefinitionId",
                table: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "AgentRuns");
        }
    }
}
