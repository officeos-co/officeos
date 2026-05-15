using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OffceOs.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAgentRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DefinitionId",
                table: "ResourceLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "ResourceLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "ResourceLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkError",
                table: "ResourceLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkPurpose",
                table: "ResourceLogs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkStatus",
                table: "ResourceLogs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO "ResourceLogs" (
                    "Id", "ResourceKind", "ResourceId", "ResourceName", "AgentId", "WorkspaceId",
                    "Time", "Type", "Severity", "Content", "CorrelationId", "WorkStatus",
                    "WorkPurpose", "DefinitionId", "StartedAt", "CompletedAt", "WorkError")
                SELECT
                    r."Id",
                    'Agent',
                    r."AgentId",
                    COALESCE(NULLIF(r."Name", ''), a."Name"),
                    r."AgentId",
                    COALESCE(r."WorkspaceId", a."WorkspaceId"),
                    r."CreatedAt",
                    'MessageIn',
                    'info',
                    r."Prompt",
                    COALESCE(r."ParentCorrelationId", replace(r."Id"::text, '-', '')),
                    r."Status",
                    r."Purpose",
                    r."DefinitionId",
                    CASE WHEN r."Status" = 'queued' THEN NULL ELSE r."UpdatedAt" END,
                    r."CompletedAt",
                    r."Error"
                FROM "AgentRuns" r
                LEFT JOIN "Agents" a ON a."Id" = r."AgentId"
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "ResourceLogs" l
                    WHERE l."RunId" = r."Id"
                      AND l."Type" = 'MessageIn'
                );
                """);

            migrationBuilder.Sql("""
                UPDATE "ResourceLogs" l
                SET
                    "ResourceKind" = 'Agent',
                    "ResourceId" = r."AgentId",
                    "ResourceName" = COALESCE(NULLIF(l."ResourceName", ''), NULLIF(r."Name", ''), a."Name"),
                    "ParentResourceKind" = NULL,
                    "ParentResourceId" = NULL,
                    "AgentId" = r."AgentId",
                    "WorkspaceId" = COALESCE(l."WorkspaceId", r."WorkspaceId", a."WorkspaceId"),
                    "CorrelationId" = COALESCE(l."CorrelationId", r."ParentCorrelationId", replace(r."Id"::text, '-', '')),
                    "WorkStatus" = CASE WHEN l."Type" = 'MessageIn' THEN COALESCE(l."WorkStatus", r."Status") ELSE l."WorkStatus" END,
                    "WorkPurpose" = COALESCE(l."WorkPurpose", r."Purpose"),
                    "DefinitionId" = COALESCE(l."DefinitionId", r."DefinitionId"),
                    "StartedAt" = CASE
                        WHEN l."Type" = 'MessageIn' AND r."Status" <> 'queued' THEN COALESCE(l."StartedAt", r."UpdatedAt")
                        ELSE l."StartedAt"
                    END,
                    "CompletedAt" = CASE
                        WHEN l."Type" = 'MessageIn' THEN COALESCE(l."CompletedAt", r."CompletedAt")
                        ELSE l."CompletedAt"
                    END,
                    "WorkError" = CASE WHEN l."Type" = 'MessageIn' THEN COALESCE(l."WorkError", r."Error") ELSE l."WorkError" END
                FROM "AgentRuns" r
                LEFT JOIN "Agents" a ON a."Id" = r."AgentId"
                WHERE l."RunId" = r."Id";
                """);

            migrationBuilder.DropTable(
                name: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "ParentRunId",
                table: "ResourceLogs");

            migrationBuilder.DropColumn(
                name: "RunId",
                table: "ResourceLogs");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLogs_AgentId_WorkStatus_Time",
                table: "ResourceLogs",
                columns: new[] { "AgentId", "WorkStatus", "Time" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLogs_WorkspaceId_WorkStatus_Time",
                table: "ResourceLogs",
                columns: new[] { "WorkspaceId", "WorkStatus", "Time" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ResourceLogs_AgentId_WorkStatus_Time",
                table: "ResourceLogs");

            migrationBuilder.DropIndex(
                name: "IX_ResourceLogs_WorkspaceId_WorkStatus_Time",
                table: "ResourceLogs");

            migrationBuilder.AddColumn<Guid>(
                name: "RunId",
                table: "ResourceLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentRunId",
                table: "ResourceLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ParentCorrelationId = table.Column<string>(type: "text", nullable: true),
                    ParentRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    Prompt = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "manual"),
                    Result = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentRuns_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentRuns_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO "AgentRuns" (
                    "Id", "AgentId", "WorkspaceId", "CompletedAt", "CreatedAt", "DefinitionId",
                    "Description", "Error", "Kind", "Name", "ParentCorrelationId", "ParentRunId",
                    "Prompt", "Purpose", "Result", "Status", "UpdatedAt")
                SELECT
                    l."Id",
                    l."AgentId",
                    l."WorkspaceId",
                    l."CompletedAt",
                    l."Time",
                    l."DefinitionId",
                    'opencode',
                    l."WorkError",
                    'opencode',
                    COALESCE(l."ResourceName", l."Id"::text),
                    l."CorrelationId",
                    NULL,
                    l."Content",
                    COALESCE(l."WorkPurpose", 'manual'),
                    NULL,
                    COALESCE(l."WorkStatus", 'completed'),
                    COALESCE(l."CompletedAt", l."StartedAt", l."Time")
                FROM "ResourceLogs" l
                WHERE l."Type" = 'MessageIn'
                  AND l."WorkStatus" IS NOT NULL
                  AND l."AgentId" IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE "ResourceLogs" l
                SET "RunId" = w."Id"
                FROM "ResourceLogs" w
                WHERE w."Type" = 'MessageIn'
                  AND w."WorkStatus" IS NOT NULL
                  AND w."CorrelationId" IS NOT NULL
                  AND l."CorrelationId" = w."CorrelationId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_AgentId",
                table: "AgentRuns",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_DefinitionId",
                table: "AgentRuns",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_ParentRunId",
                table: "AgentRuns",
                column: "ParentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_Status",
                table: "AgentRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_WorkspaceId",
                table: "AgentRuns",
                column: "WorkspaceId");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ResourceLogs");

            migrationBuilder.DropColumn(
                name: "DefinitionId",
                table: "ResourceLogs");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "ResourceLogs");

            migrationBuilder.DropColumn(
                name: "WorkError",
                table: "ResourceLogs");

            migrationBuilder.DropColumn(
                name: "WorkPurpose",
                table: "ResourceLogs");

            migrationBuilder.DropColumn(
                name: "WorkStatus",
                table: "ResourceLogs");
        }
    }
}
