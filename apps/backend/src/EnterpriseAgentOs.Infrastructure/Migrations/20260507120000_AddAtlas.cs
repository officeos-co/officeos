using EnterpriseAgentOs.Infrastructure.Common;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseAgentOs.Infrastructure.Migrations
{
    [DbContext(typeof(EaosDbContext))]
    [Migration("20260507120000_AddAtlas")]
    public partial class AddAtlas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AtlasConnectorConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WorkspaceName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RepositoriesJson = table.Column<string>(type: "jsonb", nullable: false),
                    EntitiesJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtlasConnectorConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtlasConnectorConnections_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AtlasEntityStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Entity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RecordCount = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtlasEntityStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtlasEntityStatuses_AtlasConnectorConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "AtlasConnectorConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AtlasIndexedRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Entity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SearchText = table.Column<string>(type: "text", nullable: false),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false),
                    ExternalUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtlasIndexedRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtlasIndexedRecords_AtlasConnectorConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "AtlasConnectorConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AtlasIndexJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    RecordsIndexed = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtlasIndexJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtlasIndexJobs_AtlasConnectorConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "AtlasConnectorConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AtlasRequestHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Entity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ParamsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtlasRequestHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtlasRequestHistory_AtlasConnectorConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "AtlasConnectorConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_AtlasConnectorConnections_CreatedById", table: "AtlasConnectorConnections", column: "CreatedById");
            migrationBuilder.CreateIndex(name: "IX_AtlasConnectorConnections_Provider", table: "AtlasConnectorConnections", column: "Provider");
            migrationBuilder.CreateIndex(name: "IX_AtlasEntityStatuses_ConnectionId_Entity", table: "AtlasEntityStatuses", columns: new[] { "ConnectionId", "Entity" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_AtlasIndexedRecords_ConnectionId_Entity", table: "AtlasIndexedRecords", columns: new[] { "ConnectionId", "Entity" });
            migrationBuilder.CreateIndex(name: "IX_AtlasIndexedRecords_ConnectionId_Entity_ExternalId", table: "AtlasIndexedRecords", columns: new[] { "ConnectionId", "Entity", "ExternalId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_AtlasIndexJobs_ConnectionId", table: "AtlasIndexJobs", column: "ConnectionId");
            migrationBuilder.CreateIndex(name: "IX_AtlasIndexJobs_Status", table: "AtlasIndexJobs", column: "Status");
            migrationBuilder.CreateIndex(name: "IX_AtlasRequestHistory_ConnectionId", table: "AtlasRequestHistory", column: "ConnectionId");
            migrationBuilder.CreateIndex(name: "IX_AtlasRequestHistory_CreatedAt", table: "AtlasRequestHistory", column: "CreatedAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AtlasEntityStatuses");
            migrationBuilder.DropTable(name: "AtlasIndexedRecords");
            migrationBuilder.DropTable(name: "AtlasIndexJobs");
            migrationBuilder.DropTable(name: "AtlasRequestHistory");
            migrationBuilder.DropTable(name: "AtlasConnectorConnections");
        }
    }
}
