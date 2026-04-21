#nullable disable

namespace EnterpriseAgentOs.Infrastructure.Common.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEmojiAndSourceCodeUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Emoji",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "SourceCodeUrl",
                table: "Skills");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Emoji",
                table: "Skills",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourceCodeUrl",
                table: "Skills",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }
    }
}
