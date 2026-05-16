using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OffceOs.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentRoutineCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentRoutineCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AuthKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EncryptedSecret = table.Column<string>(type: "text", nullable: false),
                    PublicMetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    ScopesJson = table.Column<string>(type: "jsonb", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentRoutineCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentRoutineCredentials_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentRoutineCredentials_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoutineCredentials_OwnerId",
                table: "AgentRoutineCredentials",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoutineCredentials_WorkspaceId_Name",
                table: "AgentRoutineCredentials",
                columns: new[] { "WorkspaceId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentRoutineCredentials");
        }
    }
}
