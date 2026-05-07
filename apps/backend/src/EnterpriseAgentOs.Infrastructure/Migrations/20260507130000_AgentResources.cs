using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseAgentOs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgentResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentSessionResourceAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Instructions = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentSessionResourceAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentSessionResourceAttachments_AgentSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AgentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentSessionResourceAttachments_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BrowserResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CurrentAgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrowserResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrowserResources_Agents_CurrentAgentId",
                        column: x => x.CurrentAgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BrowserResources_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemoryStores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryStores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemoryStores_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemoryStoreEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemoryStoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryStoreEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemoryStoreEntries_MemoryStores_MemoryStoreId",
                        column: x => x.MemoryStoreId,
                        principalTable: "MemoryStores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentSessionResourceAttachments_AgentId_ResourceType",
                table: "AgentSessionResourceAttachments",
                columns: new[] { "AgentId", "ResourceType" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentSessionResourceAttachments_SessionId_ResourceType_Reso~",
                table: "AgentSessionResourceAttachments",
                columns: new[] { "SessionId", "ResourceType", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrowserResources_CurrentAgentId",
                table: "BrowserResources",
                column: "CurrentAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_BrowserResources_OwnerId",
                table: "BrowserResources",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryStoreEntries_MemoryStoreId_Key",
                table: "MemoryStoreEntries",
                columns: new[] { "MemoryStoreId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemoryStores_OwnerId",
                table: "MemoryStores",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentSessionResourceAttachments");

            migrationBuilder.DropTable(
                name: "BrowserResources");

            migrationBuilder.DropTable(
                name: "MemoryStoreEntries");

            migrationBuilder.DropTable(
                name: "MemoryStores");
        }
    }
}
