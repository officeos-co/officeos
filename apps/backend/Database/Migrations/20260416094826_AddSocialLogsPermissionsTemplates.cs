using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseAgentOs.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialLogsPermissionsTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceCodeUrl",
                table: "Skills",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Tool = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Integration = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Channel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    InputTokens = table.Column<int>(type: "integer", nullable: true),
                    OutputTokens = table.Column<int>(type: "integer", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentLogs_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Prompt = table.Column<string>(type: "text", nullable: false),
                    IntegrationsJson = table.Column<string>(type: "text", nullable: false),
                    ChannelsJson = table.Column<string>(type: "text", nullable: false),
                    IsBuiltin = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentTemplates_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AgentToolPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ToolName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Permission = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentToolPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentToolPermissions_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillComments_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SkillComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillLikes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillLikes_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SkillLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentLogs_AgentId",
                table: "AgentLogs",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentLogs_AgentId_Time",
                table: "AgentLogs",
                columns: new[] { "AgentId", "Time" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentLogs_CorrelationId",
                table: "AgentLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentTemplates_Name",
                table: "AgentTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentTemplates_OwnerId",
                table: "AgentTemplates",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolPermissions_AgentId",
                table: "AgentToolPermissions",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolPermissions_AgentId_SkillName_ToolName",
                table: "AgentToolPermissions",
                columns: new[] { "AgentId", "SkillName", "ToolName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillComments_SkillId",
                table: "SkillComments",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillComments_SkillId_CreatedAt",
                table: "SkillComments",
                columns: new[] { "SkillId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillComments_UserId",
                table: "SkillComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillLikes_SkillId",
                table: "SkillLikes",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillLikes_UserId_SkillId",
                table: "SkillLikes",
                columns: new[] { "UserId", "SkillId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentLogs");

            migrationBuilder.DropTable(
                name: "AgentTemplates");

            migrationBuilder.DropTable(
                name: "AgentToolPermissions");

            migrationBuilder.DropTable(
                name: "SkillComments");

            migrationBuilder.DropTable(
                name: "SkillLikes");

            migrationBuilder.DropColumn(
                name: "SourceCodeUrl",
                table: "Skills");
        }
    }
}
