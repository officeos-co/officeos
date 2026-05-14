#nullable disable

namespace OffceOs.Database.Migrations;

public partial class ResourceLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameTable(
            name: "AgentLogs",
            newName: "ResourceLogs");

        migrationBuilder.RenameIndex(
            name: "IX_AgentLogs_WorkspaceId",
            table: "ResourceLogs",
            newName: "IX_ResourceLogs_WorkspaceId");

        migrationBuilder.RenameIndex(
            name: "IX_AgentLogs_CorrelationId",
            table: "ResourceLogs",
            newName: "IX_ResourceLogs_CorrelationId");

        migrationBuilder.RenameIndex(
            name: "IX_AgentLogs_ChannelConnectionId",
            table: "ResourceLogs",
            newName: "IX_ResourceLogs_ChannelConnectionId");

        migrationBuilder.RenameIndex(
            name: "IX_AgentLogs_AgentId_Time",
            table: "ResourceLogs",
            newName: "IX_ResourceLogs_AgentId_Time");

        migrationBuilder.RenameIndex(
            name: "IX_AgentLogs_AgentId",
            table: "ResourceLogs",
            newName: "IX_ResourceLogs_AgentId");

        migrationBuilder.AddColumn<string>(
            name: "ResourceKind",
            table: "ResourceLogs",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "Agent");

        migrationBuilder.AddColumn<Guid>(
            name: "ResourceId",
            table: "ResourceLogs",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ResourceName",
            table: "ResourceLogs",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ParentResourceKind",
            table: "ResourceLogs",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ParentResourceId",
            table: "ResourceLogs",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Severity",
            table: "ResourceLogs",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "info");

        migrationBuilder.AddColumn<string>(
            name: "MetadataJson",
            table: "ResourceLogs",
            type: "text",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE "ResourceLogs"
            SET
                "ResourceKind" = CASE
                    WHEN "RunId" IS NOT NULL THEN 'Run'
                    WHEN "ChannelConnectionId" IS NOT NULL THEN 'Channel'
                    ELSE 'Agent'
                END,
                "ResourceId" = COALESCE("RunId", "ChannelConnectionId", "AgentId"),
                "ParentResourceKind" = CASE
                    WHEN "RunId" IS NOT NULL AND "AgentId" IS NOT NULL THEN 'Agent'
                    ELSE NULL
                END,
                "ParentResourceId" = CASE
                    WHEN "RunId" IS NOT NULL THEN "AgentId"
                    ELSE NULL
                END,
                "Severity" = CASE
                    WHEN "Type" LIKE 'Error%' OR "Type" = 'Error' THEN 'error'
                    ELSE 'info'
                END;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_ResourceLogs_WorkspaceId_ResourceKind_ResourceId_Time",
            table: "ResourceLogs",
            columns: new[] { "WorkspaceId", "ResourceKind", "ResourceId", "Time" });

        migrationBuilder.CreateIndex(
            name: "IX_ResourceLogs_WorkspaceId_ResourceKind_ResourceName_Time",
            table: "ResourceLogs",
            columns: new[] { "WorkspaceId", "ResourceKind", "ResourceName", "Time" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ResourceLogs_WorkspaceId_ResourceKind_ResourceId_Time",
            table: "ResourceLogs");

        migrationBuilder.DropIndex(
            name: "IX_ResourceLogs_WorkspaceId_ResourceKind_ResourceName_Time",
            table: "ResourceLogs");

        migrationBuilder.DropColumn(
            name: "ResourceKind",
            table: "ResourceLogs");

        migrationBuilder.DropColumn(
            name: "ResourceId",
            table: "ResourceLogs");

        migrationBuilder.DropColumn(
            name: "ResourceName",
            table: "ResourceLogs");

        migrationBuilder.DropColumn(
            name: "ParentResourceKind",
            table: "ResourceLogs");

        migrationBuilder.DropColumn(
            name: "ParentResourceId",
            table: "ResourceLogs");

        migrationBuilder.DropColumn(
            name: "Severity",
            table: "ResourceLogs");

        migrationBuilder.DropColumn(
            name: "MetadataJson",
            table: "ResourceLogs");

        migrationBuilder.RenameTable(
            name: "ResourceLogs",
            newName: "AgentLogs");

        migrationBuilder.RenameIndex(
            name: "IX_ResourceLogs_WorkspaceId",
            table: "AgentLogs",
            newName: "IX_AgentLogs_WorkspaceId");

        migrationBuilder.RenameIndex(
            name: "IX_ResourceLogs_CorrelationId",
            table: "AgentLogs",
            newName: "IX_AgentLogs_CorrelationId");

        migrationBuilder.RenameIndex(
            name: "IX_ResourceLogs_ChannelConnectionId",
            table: "AgentLogs",
            newName: "IX_AgentLogs_ChannelConnectionId");

        migrationBuilder.RenameIndex(
            name: "IX_ResourceLogs_AgentId_Time",
            table: "AgentLogs",
            newName: "IX_AgentLogs_AgentId_Time");

        migrationBuilder.RenameIndex(
            name: "IX_ResourceLogs_AgentId",
            table: "AgentLogs",
            newName: "IX_AgentLogs_AgentId");
    }
}
