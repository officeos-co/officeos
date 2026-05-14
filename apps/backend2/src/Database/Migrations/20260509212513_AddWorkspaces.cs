#nullable disable

namespace OffceOs.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Integrations_OwnerId",
                table: "Integrations");

            migrationBuilder.DropIndex(
                name: "IX_Integrations_OwnerId_Name",
                table: "Integrations");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationCredentials_OwnerId_IntegrationName",
                table: "IntegrationCredentials");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationConnections_CreatedById",
                table: "IntegrationConnections");

            migrationBuilder.DropIndex(
                name: "IX_ChannelConnections_CreatedById",
                table: "ChannelConnections");

            migrationBuilder.DropIndex(
                name: "IX_Agents_OwnerId",
                table: "Agents");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentWorkspaceId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "MemoryStores",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "Integrations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "IntegrationCredentials",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "IntegrationConnections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "ChannelConnections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "BrowserResources",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "Agents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "AgentRuns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "AgentLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workspaces_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "Workspaces" ("Id", "UserId", "Name", "CreatedAt", "UpdatedAt")
                SELECT
                    (substr(h, 1, 8) || '-' || substr(h, 9, 4) || '-' || substr(h, 13, 4) || '-' || substr(h, 17, 4) || '-' || substr(h, 21, 12))::uuid,
                    "Id",
                    'Default',
                    NOW(),
                    NOW()
                FROM (
                    SELECT "Id", md5("Id"::text || ':default-workspace') AS h
                    FROM "Users"
                ) AS defaults;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Users" u
                SET "CurrentWorkspaceId" = w."Id"
                FROM "Workspaces" w
                WHERE w."UserId" = u."Id";
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Agents" a
                SET "WorkspaceId" = w."Id"
                FROM "Workspaces" w
                WHERE a."OwnerId" = w."UserId";

                UPDATE "ChannelConnections" c
                SET "WorkspaceId" = w."Id"
                FROM "Workspaces" w
                WHERE c."CreatedById" = w."UserId";

                UPDATE "MemoryStores" s
                SET "WorkspaceId" = w."Id"
                FROM "Workspaces" w
                WHERE s."OwnerId" = w."UserId";

                UPDATE "BrowserResources" r
                SET "WorkspaceId" = w."Id"
                FROM "Workspaces" w
                WHERE r."OwnerId" = w."UserId";

                UPDATE "Integrations" i
                SET "WorkspaceId" = w."Id"
                FROM "Workspaces" w
                WHERE i."OwnerId" = w."UserId";

                UPDATE "IntegrationCredentials" c
                SET "WorkspaceId" = w."Id"
                FROM "Workspaces" w
                WHERE c."OwnerId" = w."UserId";

                UPDATE "IntegrationConnections" c
                SET "WorkspaceId" = w."Id"
                FROM "Workspaces" w
                WHERE c."CreatedById" = w."UserId";

                UPDATE "AgentRuns" r
                SET "WorkspaceId" = a."WorkspaceId"
                FROM "Agents" a
                WHERE r."AgentId" = a."Id";

                UPDATE "AgentLogs" l
                SET "WorkspaceId" = a."WorkspaceId"
                FROM "Agents" a
                WHERE l."AgentId" = a."Id";

                UPDATE "AgentLogs" l
                SET "WorkspaceId" = c."WorkspaceId"
                FROM "ChannelConnections" c
                WHERE l."WorkspaceId" IS NULL
                    AND l."ChannelConnectionId" = c."Id";
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkspaceId",
                table: "MemoryStores",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkspaceId",
                table: "IntegrationConnections",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkspaceId",
                table: "BrowserResources",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CurrentWorkspaceId",
                table: "Users",
                column: "CurrentWorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryStores_OwnerId_WorkspaceId",
                table: "MemoryStores",
                columns: new[] { "OwnerId", "WorkspaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_MemoryStores_WorkspaceId",
                table: "MemoryStores",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_OwnerId_WorkspaceId",
                table: "Integrations",
                columns: new[] { "OwnerId", "WorkspaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_OwnerId_WorkspaceId_Name",
                table: "Integrations",
                columns: new[] { "OwnerId", "WorkspaceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_WorkspaceId",
                table: "Integrations",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationCredentials_OwnerId_WorkspaceId_IntegrationName",
                table: "IntegrationCredentials",
                columns: new[] { "OwnerId", "WorkspaceId", "IntegrationName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationCredentials_WorkspaceId",
                table: "IntegrationCredentials",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_CreatedById_WorkspaceId",
                table: "IntegrationConnections",
                columns: new[] { "CreatedById", "WorkspaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_WorkspaceId",
                table: "IntegrationConnections",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelConnections_CreatedById_WorkspaceId",
                table: "ChannelConnections",
                columns: new[] { "CreatedById", "WorkspaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelConnections_WorkspaceId",
                table: "ChannelConnections",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_BrowserResources_OwnerId_WorkspaceId",
                table: "BrowserResources",
                columns: new[] { "OwnerId", "WorkspaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_BrowserResources_WorkspaceId",
                table: "BrowserResources",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_OwnerId_WorkspaceId",
                table: "Agents",
                columns: new[] { "OwnerId", "WorkspaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Agents_WorkspaceId",
                table: "Agents",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_WorkspaceId",
                table: "AgentRuns",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentLogs_WorkspaceId",
                table: "AgentLogs",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_UserId_Name",
                table: "Workspaces",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentLogs_Workspaces_WorkspaceId",
                table: "AgentLogs",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentRuns_Workspaces_WorkspaceId",
                table: "AgentRuns",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Workspaces_WorkspaceId",
                table: "Agents",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BrowserResources_Workspaces_WorkspaceId",
                table: "BrowserResources",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelConnections_Workspaces_WorkspaceId",
                table: "ChannelConnections",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IntegrationConnections_Workspaces_WorkspaceId",
                table: "IntegrationConnections",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IntegrationCredentials_Workspaces_WorkspaceId",
                table: "IntegrationCredentials",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Integrations_Workspaces_WorkspaceId",
                table: "Integrations",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MemoryStores_Workspaces_WorkspaceId",
                table: "MemoryStores",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Workspaces_CurrentWorkspaceId",
                table: "Users",
                column: "CurrentWorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentLogs_Workspaces_WorkspaceId",
                table: "AgentLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentRuns_Workspaces_WorkspaceId",
                table: "AgentRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Workspaces_WorkspaceId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_BrowserResources_Workspaces_WorkspaceId",
                table: "BrowserResources");

            migrationBuilder.DropForeignKey(
                name: "FK_ChannelConnections_Workspaces_WorkspaceId",
                table: "ChannelConnections");

            migrationBuilder.DropForeignKey(
                name: "FK_IntegrationConnections_Workspaces_WorkspaceId",
                table: "IntegrationConnections");

            migrationBuilder.DropForeignKey(
                name: "FK_IntegrationCredentials_Workspaces_WorkspaceId",
                table: "IntegrationCredentials");

            migrationBuilder.DropForeignKey(
                name: "FK_Integrations_Workspaces_WorkspaceId",
                table: "Integrations");

            migrationBuilder.DropForeignKey(
                name: "FK_MemoryStores_Workspaces_WorkspaceId",
                table: "MemoryStores");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Workspaces_CurrentWorkspaceId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Workspaces");

            migrationBuilder.DropIndex(
                name: "IX_Users_CurrentWorkspaceId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_MemoryStores_OwnerId_WorkspaceId",
                table: "MemoryStores");

            migrationBuilder.DropIndex(
                name: "IX_MemoryStores_WorkspaceId",
                table: "MemoryStores");

            migrationBuilder.DropIndex(
                name: "IX_Integrations_OwnerId_WorkspaceId",
                table: "Integrations");

            migrationBuilder.DropIndex(
                name: "IX_Integrations_OwnerId_WorkspaceId_Name",
                table: "Integrations");

            migrationBuilder.DropIndex(
                name: "IX_Integrations_WorkspaceId",
                table: "Integrations");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationCredentials_OwnerId_WorkspaceId_IntegrationName",
                table: "IntegrationCredentials");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationCredentials_WorkspaceId",
                table: "IntegrationCredentials");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationConnections_CreatedById_WorkspaceId",
                table: "IntegrationConnections");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationConnections_WorkspaceId",
                table: "IntegrationConnections");

            migrationBuilder.DropIndex(
                name: "IX_ChannelConnections_CreatedById_WorkspaceId",
                table: "ChannelConnections");

            migrationBuilder.DropIndex(
                name: "IX_ChannelConnections_WorkspaceId",
                table: "ChannelConnections");

            migrationBuilder.DropIndex(
                name: "IX_BrowserResources_OwnerId_WorkspaceId",
                table: "BrowserResources");

            migrationBuilder.DropIndex(
                name: "IX_BrowserResources_WorkspaceId",
                table: "BrowserResources");

            migrationBuilder.DropIndex(
                name: "IX_Agents_OwnerId_WorkspaceId",
                table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Agents_WorkspaceId",
                table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_AgentRuns_WorkspaceId",
                table: "AgentRuns");

            migrationBuilder.DropIndex(
                name: "IX_AgentLogs_WorkspaceId",
                table: "AgentLogs");

            migrationBuilder.DropColumn(
                name: "CurrentWorkspaceId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "MemoryStores");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "IntegrationConnections");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "ChannelConnections");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "BrowserResources");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "AgentLogs");

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_OwnerId",
                table: "Integrations",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_OwnerId_Name",
                table: "Integrations",
                columns: new[] { "OwnerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationCredentials_OwnerId_IntegrationName",
                table: "IntegrationCredentials",
                columns: new[] { "OwnerId", "IntegrationName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_CreatedById",
                table: "IntegrationConnections",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelConnections_CreatedById",
                table: "ChannelConnections",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_OwnerId",
                table: "Agents",
                column: "OwnerId");
        }
    }
}
