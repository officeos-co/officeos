using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseAgentOs.Infrastructure.Common.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBindingReplyContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastChannelId",
                table: "AgentChannelBindings");

            migrationBuilder.DropColumn(
                name: "LastSenderIdentifier",
                table: "AgentChannelBindings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastChannelId",
                table: "AgentChannelBindings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSenderIdentifier",
                table: "AgentChannelBindings",
                type: "text",
                nullable: true);
        }
    }
}
