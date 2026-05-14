using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OffceOs.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIntegrationIndexing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationActivity");

            migrationBuilder.DropTable(
                name: "IntegrationIndexedRecords");

            migrationBuilder.DropTable(
                name: "IntegrationIndexEntityStatuses");

            migrationBuilder.DropTable(
                name: "IntegrationIndexJobs");

            migrationBuilder.DropTable(
                name: "IntegrationRequestHistory");

            migrationBuilder.DropTable(
                name: "IntegrationConnections");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntegrationConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntitiesJson = table.Column<string>(type: "jsonb", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RepositoriesJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WorkspaceName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationConnections_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationConnections_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationActivity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Entity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationActivity_IntegrationConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "IntegrationConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationIndexedRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Entity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ExternalUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false),
                    SearchText = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationIndexedRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationIndexedRecords_IntegrationConnections_Connection~",
                        column: x => x.ConnectionId,
                        principalTable: "IntegrationConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationIndexEntityStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Entity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RecordCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationIndexEntityStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationIndexEntityStatuses_IntegrationConnections_Conne~",
                        column: x => x.ConnectionId,
                        principalTable: "IntegrationConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationIndexJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    RecordsIndexed = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationIndexJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationIndexJobs_IntegrationConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "IntegrationConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationRequestHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false),
                    Entity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    ParamsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationRequestHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationRequestHistory_IntegrationConnections_Connection~",
                        column: x => x.ConnectionId,
                        principalTable: "IntegrationConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationActivity_ConnectionId",
                table: "IntegrationActivity",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationActivity_CreatedAt",
                table: "IntegrationActivity",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_CreatedById_WorkspaceId",
                table: "IntegrationConnections",
                columns: new[] { "CreatedById", "WorkspaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_Provider",
                table: "IntegrationConnections",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_WorkspaceId",
                table: "IntegrationConnections",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationIndexedRecords_ConnectionId_Entity",
                table: "IntegrationIndexedRecords",
                columns: new[] { "ConnectionId", "Entity" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationIndexedRecords_ConnectionId_Entity_ExternalId",
                table: "IntegrationIndexedRecords",
                columns: new[] { "ConnectionId", "Entity", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationIndexEntityStatuses_ConnectionId_Entity",
                table: "IntegrationIndexEntityStatuses",
                columns: new[] { "ConnectionId", "Entity" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationIndexJobs_ConnectionId",
                table: "IntegrationIndexJobs",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationIndexJobs_Status",
                table: "IntegrationIndexJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRequestHistory_ConnectionId",
                table: "IntegrationRequestHistory",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRequestHistory_CreatedAt",
                table: "IntegrationRequestHistory",
                column: "CreatedAt");
        }
    }
}
