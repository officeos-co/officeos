#nullable disable

namespace OffceOs.Database.Migrations
{
    /// <inheritdoc />
    public partial class ImmutableAgentDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentToolPermissions");

            migrationBuilder.AddColumn<Guid>(
                name: "ActiveDefinitionId",
                table: "Agents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SystemPrompt = table.Column<string>(type: "text", nullable: true),
                    ConfigJson = table.Column<string>(type: "jsonb", nullable: false),
                    ConfigHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentDefinitions_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentDefinitions_AgentId",
                table: "AgentDefinitions",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentDefinitions_AgentId_Version",
                table: "AgentDefinitions",
                columns: new[] { "AgentId", "Version" },
                unique: true);

            migrationBuilder.Sql(
                """
                WITH seed AS (
                    SELECT
                        "Id" AS "AgentId",
                        (
                            substr(md5("Id"::text || ':definition:1'), 1, 8) || '-' ||
                            substr(md5("Id"::text || ':definition:1'), 9, 4) || '-' ||
                            substr(md5("Id"::text || ':definition:1'), 13, 4) || '-' ||
                            substr(md5("Id"::text || ':definition:1'), 17, 4) || '-' ||
                            substr(md5("Id"::text || ':definition:1'), 21, 12)
                        )::uuid AS "DefinitionId"
                    FROM "Agents"
                )
                INSERT INTO "AgentDefinitions" (
                    "Id",
                    "AgentId",
                    "Version",
                    "Name",
                    "Description",
                    "Provider",
                    "Model",
                    "SystemPrompt",
                    "ConfigJson",
                    "ConfigHash",
                    "CreatedBy",
                    "CreatedAt")
                SELECT
                    seed."DefinitionId",
                    agent."Id",
                    1,
                    agent."Name",
                    NULL,
                    agent."Provider",
                    agent."Model",
                    agent."Prompt",
                    jsonb_build_object(
                        'name', agent."Name",
                        'description', NULL,
                        'model', COALESCE(agent."Model", 'auto'),
                        'system', agent."Prompt",
                        'mcp_servers', COALESCE((
                            SELECT jsonb_agg(jsonb_build_object(
                                'name', integration."IntegrationName",
                                'type', 'registered',
                                'url', NULL)
                                ORDER BY integration."IntegrationName")
                            FROM "AgentIntegrations" integration
                            WHERE integration."AgentId" = agent."Id"), '[]'::jsonb),
                        'tools', jsonb_build_array(jsonb_build_object(
                                'type', 'agent_toolset_20260401',
                                'default_config', jsonb_build_object(
                                    'permission_policy', jsonb_build_object('type', 'always_allow'))))
                            || COALESCE((
                                SELECT jsonb_agg(jsonb_build_object(
                                    'type', 'mcp_toolset',
                                    'mcp_server_name', integration."IntegrationName",
                                    'default_config', jsonb_build_object(
                                        'permission_policy', jsonb_build_object('type', 'always_allow')))
                                    ORDER BY integration."IntegrationName")
                                FROM "AgentIntegrations" integration
                                WHERE integration."AgentId" = agent."Id"), '[]'::jsonb),
                        'metadata', jsonb_build_object('migrated_from', 'agent_record')),
                    'migrated',
                    agent."OwnerId",
                    agent."CreatedAt"
                FROM "Agents" agent
                JOIN seed ON seed."AgentId" = agent."Id";

                UPDATE "Agents" agent
                SET "ActiveDefinitionId" = definition."Id"
                FROM "AgentDefinitions" definition
                WHERE definition."AgentId" = agent."Id"
                    AND definition."Version" = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentDefinitions");

            migrationBuilder.DropColumn(
                name: "ActiveDefinitionId",
                table: "Agents");

            migrationBuilder.CreateTable(
                name: "AgentToolPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Permission = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SkillName = table.Column<string>(type: "text", nullable: false),
                    ToolName = table.Column<string>(type: "text", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolPermissions_AgentId",
                table: "AgentToolPermissions",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolPermissions_AgentId_SkillName_ToolName",
                table: "AgentToolPermissions",
                columns: new[] { "AgentId", "SkillName", "ToolName" },
                unique: true);
        }
    }
}
