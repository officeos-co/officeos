using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OffceOs.Database.Migrations
{
    /// <inheritdoc />
    public partial class SessionsAsRunsAndRepositoryLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgentSessionContexts_AgentId",
                table: "AgentSessionContexts");

            migrationBuilder.DropColumn(
                name: "MessageCount",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "PodName",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "ServiceUrl",
                table: "Agents");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "AgentSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "ResourceLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "AgentSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "AgentSessions"
                SET "CompletedAt" = "EndedAt"
                WHERE "EndedAt" IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "AgentSessions");

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "AgentSessions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "DefinitionId",
                table: "AgentSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "AgentSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Input",
                table: "AgentSessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "AgentSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "AgentSessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "RoutineId",
                table: "AgentSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "AgentSessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TriggerId",
                table: "AgentSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriggerPayloadJson",
                table: "AgentSessions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "AgentSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "AgentSessions" AS s
                SET
                    "OwnerId" = a."OwnerId",
                    "WorkspaceId" = a."WorkspaceId",
                    "Source" = 'manual',
                    "Purpose" = 'manual',
                    "CorrelationId" = s."Id"::text,
                    "Input" = 'Migrated legacy session.',
                    "Status" = CASE
                        WHEN s."Status" = 'active' THEN 'running'
                        WHEN s."Status" = 'ended' THEN 'completed'
                        ELSE s."Status"
                    END
                FROM "Agents" AS a
                WHERE s."AgentId" = a."Id";
                """);

            migrationBuilder.Sql("""DELETE FROM "AgentSessionContexts";""");

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "AgentSessionContexts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "RepositoryBaseBranch",
                table: "AgentRoutines",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepositoryCloneUrl",
                table: "AgentRoutines",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepositoryCredentialRef",
                table: "AgentRoutines",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepositoryFullName",
                table: "AgentRoutines",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentSessionPullRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: true),
                    Branch = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CommitSha = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentSessionPullRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentSessionPullRequests_AgentSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AgentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentSessionRepositories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CloneUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    BaseBranch = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CredentialRef = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Branch = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentSessionRepositories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentSessionRepositories_AgentSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AgentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentSessionRuntimes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SandboxId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ServiceUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentSessionRuntimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentSessionRuntimes_AgentSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AgentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLogs_SessionId",
                table: "ResourceLogs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLogs_SessionId_Time_Id",
                table: "ResourceLogs",
                columns: new[] { "SessionId", "Time", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLogs_SessionId_WorkStatus_Time",
                table: "ResourceLogs",
                columns: new[] { "SessionId", "WorkStatus", "Time" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentSessions_AgentId_CreatedAt",
                table: "AgentSessions",
                columns: new[] { "AgentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentSessions_CorrelationId",
                table: "AgentSessions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentSessions_RoutineId",
                table: "AgentSessions",
                column: "RoutineId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentSessions_WorkspaceId_Status",
                table: "AgentSessions",
                columns: new[] { "WorkspaceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentSessionContexts_AgentId",
                table: "AgentSessionContexts",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentSessionContexts_SessionId",
                table: "AgentSessionContexts",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentSessionPullRequests_SessionId",
                table: "AgentSessionPullRequests",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentSessionRepositories_SessionId",
                table: "AgentSessionRepositories",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentSessionRuntimes_SessionId",
                table: "AgentSessionRuntimes",
                column: "SessionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentSessionContexts_AgentSessions_SessionId",
                table: "AgentSessionContexts",
                column: "SessionId",
                principalTable: "AgentSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentSessions_Workspaces_WorkspaceId",
                table: "AgentSessions",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceLogs_AgentSessions_SessionId",
                table: "ResourceLogs",
                column: "SessionId",
                principalTable: "AgentSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentSessionContexts_AgentSessions_SessionId",
                table: "AgentSessionContexts");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentSessions_Workspaces_WorkspaceId",
                table: "AgentSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_ResourceLogs_AgentSessions_SessionId",
                table: "ResourceLogs");

            migrationBuilder.DropTable(
                name: "AgentSessionPullRequests");

            migrationBuilder.DropTable(
                name: "AgentSessionRepositories");

            migrationBuilder.DropTable(
                name: "AgentSessionRuntimes");

            migrationBuilder.DropIndex(
                name: "IX_ResourceLogs_SessionId",
                table: "ResourceLogs");

            migrationBuilder.DropIndex(
                name: "IX_ResourceLogs_SessionId_Time_Id",
                table: "ResourceLogs");

            migrationBuilder.DropIndex(
                name: "IX_ResourceLogs_SessionId_WorkStatus_Time",
                table: "ResourceLogs");

            migrationBuilder.DropIndex(
                name: "IX_AgentSessions_AgentId_CreatedAt",
                table: "AgentSessions");

            migrationBuilder.DropIndex(
                name: "IX_AgentSessions_CorrelationId",
                table: "AgentSessions");

            migrationBuilder.DropIndex(
                name: "IX_AgentSessions_RoutineId",
                table: "AgentSessions");

            migrationBuilder.DropIndex(
                name: "IX_AgentSessions_WorkspaceId_Status",
                table: "AgentSessions");

            migrationBuilder.DropIndex(
                name: "IX_AgentSessionContexts_AgentId",
                table: "AgentSessionContexts");

            migrationBuilder.DropIndex(
                name: "IX_AgentSessionContexts_SessionId",
                table: "AgentSessionContexts");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "ResourceLogs");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAt",
                table: "AgentSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "AgentSessions"
                SET "EndedAt" = "CompletedAt"
                WHERE "CompletedAt" IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "DefinitionId",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "Error",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "Input",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "RoutineId",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "TriggerId",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "TriggerPayloadJson",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "AgentSessionContexts");

            migrationBuilder.DropColumn(
                name: "RepositoryBaseBranch",
                table: "AgentRoutines");

            migrationBuilder.DropColumn(
                name: "RepositoryCloneUrl",
                table: "AgentRoutines");

            migrationBuilder.DropColumn(
                name: "RepositoryCredentialRef",
                table: "AgentRoutines");

            migrationBuilder.DropColumn(
                name: "RepositoryFullName",
                table: "AgentRoutines");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "AgentSessions");

            migrationBuilder.AddColumn<int>(
                name: "MessageCount",
                table: "AgentSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PodName",
                table: "Agents",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceUrl",
                table: "Agents",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentSessionContexts_AgentId",
                table: "AgentSessionContexts",
                column: "AgentId",
                unique: true);
        }
    }
}
