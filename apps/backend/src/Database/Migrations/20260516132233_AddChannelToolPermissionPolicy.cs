using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OffceOs.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelToolPermissionPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ToolPermissionPolicyJson",
                table: "ChannelConnections",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ToolPermissionPolicyJson",
                table: "ChannelConnections");
        }
    }
}
