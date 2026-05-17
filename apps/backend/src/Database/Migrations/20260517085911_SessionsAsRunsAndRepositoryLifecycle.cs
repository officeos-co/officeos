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

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "AgentSessions");

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "ResourceLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommitSha",
                table: "AgentSessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "AgentSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "AgentSessions",
                type: "timestamp with time zone",
                nullable: true);

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

            migrationBuilder.AddColumn<int>(
                name: "PullRequestNumber",
                table: "AgentSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PullRequestUrl",
                table: "AgentSessions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "AgentSessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RepositoryBaseBranch",
                table: "AgentSessions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepositoryBranch",
                table: "AgentSessions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepositoryCloneUrl",
                table: "AgentSessions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepositoryCredentialRef",
                table: "AgentSessions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepositoryFullName",
                table: "AgentSessions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RoutineId",
                table: "AgentSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SandboxId",
                table: "AgentSessions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceUrl",
                table: "AgentSessions",
                type: "character varying(512)",
                maxLength: 512,
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

            migrationBuilder.Sql("""DELETE FROM "AgentSessionContexts";""");

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "AgentSessionContexts",
                type: "uuid",
                nullable: false);

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

            migrationBuilder.Sql(
                """
                UPDATE "AgentSessions"
                SET
                    "Status" = CASE
                        WHEN "Status" = 'active' THEN 'running'
                        WHEN "Status" = 'ended' THEN 'completed'
                        ELSE "Status"
                    END,
                    "Source" = CASE WHEN "Source" = '' THEN 'manual' ELSE "Source" END,
                    "Purpose" = CASE WHEN "Purpose" = '' THEN 'manual' ELSE "Purpose" END,
                    "CorrelationId" = CASE WHEN "CorrelationId" = '' THEN replace("Id"::text, '-', '') ELSE "CorrelationId" END,
                    "Input" = CASE WHEN "Input" = '' THEN 'Migrated legacy session.' ELSE "Input" END
                """);

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

            migrationBuilder.DropColumn(
                name: "CommitSha",
                table: "AgentSessions");

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
                name: "PullRequestNumber",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "PullRequestUrl",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "RepositoryBaseBranch",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "RepositoryBranch",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "RepositoryCloneUrl",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "RepositoryCredentialRef",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "RepositoryFullName",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "RoutineId",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "SandboxId",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "ServiceUrl",
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

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAt",
                table: "AgentSessions",
                type: "timestamp with time zone",
                nullable: true);

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
