using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseAgentOs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChannelConnectionLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChannelConnectionId",
                table: "AgentLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentLogs_ChannelConnectionId",
                table: "AgentLogs",
                column: "ChannelConnectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgentLogs_ChannelConnectionId",
                table: "AgentLogs");

            migrationBuilder.DropColumn(
                name: "ChannelConnectionId",
                table: "AgentLogs");
        }
    }
}
