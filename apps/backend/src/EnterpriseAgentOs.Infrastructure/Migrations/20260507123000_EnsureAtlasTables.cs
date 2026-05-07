using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseAgentOs.Infrastructure.Migrations
{
    [Migration("20260507123000_EnsureAtlasTables")]
    public partial class EnsureAtlasTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "AtlasConnectorConnections" (
                    "Id" uuid NOT NULL,
                    "Provider" character varying(32) NOT NULL,
                    "WorkspaceName" character varying(128) NOT NULL,
                    "DisplayName" character varying(200) NOT NULL,
                    "RepositoriesJson" jsonb NOT NULL,
                    "EntitiesJson" jsonb NOT NULL,
                    "Status" character varying(32) NOT NULL,
                    "Error" text NULL,
                    "CreatedById" uuid NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_AtlasConnectorConnections" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_AtlasConnectorConnections_Users_CreatedById"
                        FOREIGN KEY ("CreatedById") REFERENCES "Users" ("Id") ON DELETE RESTRICT
                );

                CREATE TABLE IF NOT EXISTS "AtlasEntityStatuses" (
                    "Id" uuid NOT NULL,
                    "ConnectionId" uuid NOT NULL,
                    "Entity" character varying(64) NOT NULL,
                    "Status" character varying(32) NOT NULL,
                    "RecordCount" integer NOT NULL,
                    "Error" text NULL,
                    "LastSyncedAt" timestamp with time zone NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_AtlasEntityStatuses" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_AtlasEntityStatuses_AtlasConnectorConnections_ConnectionId"
                        FOREIGN KEY ("ConnectionId") REFERENCES "AtlasConnectorConnections" ("Id") ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS "AtlasIndexedRecords" (
                    "Id" uuid NOT NULL,
                    "ConnectionId" uuid NOT NULL,
                    "Entity" character varying(64) NOT NULL,
                    "ExternalId" character varying(512) NOT NULL,
                    "Title" character varying(512) NOT NULL,
                    "SearchText" text NOT NULL,
                    "RawJson" jsonb NOT NULL,
                    "ExternalUpdatedAt" timestamp with time zone NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_AtlasIndexedRecords" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_AtlasIndexedRecords_AtlasConnectorConnections_ConnectionId"
                        FOREIGN KEY ("ConnectionId") REFERENCES "AtlasConnectorConnections" ("Id") ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS "AtlasIndexJobs" (
                    "Id" uuid NOT NULL,
                    "ConnectionId" uuid NOT NULL,
                    "Status" character varying(32) NOT NULL,
                    "Error" text NULL,
                    "RecordsIndexed" integer NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "StartedAt" timestamp with time zone NULL,
                    "CompletedAt" timestamp with time zone NULL,
                    CONSTRAINT "PK_AtlasIndexJobs" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_AtlasIndexJobs_AtlasConnectorConnections_ConnectionId"
                        FOREIGN KEY ("ConnectionId") REFERENCES "AtlasConnectorConnections" ("Id") ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS "AtlasRequestHistory" (
                    "Id" uuid NOT NULL,
                    "ConnectionId" uuid NOT NULL,
                    "Type" character varying(16) NOT NULL,
                    "Entity" character varying(64) NOT NULL,
                    "Action" character varying(64) NOT NULL,
                    "ParamsJson" jsonb NOT NULL,
                    "Success" boolean NOT NULL,
                    "DurationMs" integer NOT NULL,
                    "Error" text NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_AtlasRequestHistory" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_AtlasRequestHistory_AtlasConnectorConnections_ConnectionId"
                        FOREIGN KEY ("ConnectionId") REFERENCES "AtlasConnectorConnections" ("Id") ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS "AtlasActivity" (
                    "Id" uuid NOT NULL,
                    "ConnectionId" uuid NOT NULL,
                    "Type" character varying(64) NOT NULL,
                    "Entity" character varying(64) NULL,
                    "Message" character varying(512) NOT NULL,
                    "DetailsJson" jsonb NOT NULL,
                    "Success" boolean NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_AtlasActivity" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_AtlasActivity_AtlasConnectorConnections_ConnectionId"
                        FOREIGN KEY ("ConnectionId") REFERENCES "AtlasConnectorConnections" ("Id") ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS "IX_AtlasConnectorConnections_CreatedById"
                    ON "AtlasConnectorConnections" ("CreatedById");
                CREATE INDEX IF NOT EXISTS "IX_AtlasConnectorConnections_Provider"
                    ON "AtlasConnectorConnections" ("Provider");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_AtlasEntityStatuses_ConnectionId_Entity"
                    ON "AtlasEntityStatuses" ("ConnectionId", "Entity");
                CREATE INDEX IF NOT EXISTS "IX_AtlasIndexedRecords_ConnectionId_Entity"
                    ON "AtlasIndexedRecords" ("ConnectionId", "Entity");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_AtlasIndexedRecords_ConnectionId_Entity_ExternalId"
                    ON "AtlasIndexedRecords" ("ConnectionId", "Entity", "ExternalId");
                CREATE INDEX IF NOT EXISTS "IX_AtlasIndexJobs_ConnectionId"
                    ON "AtlasIndexJobs" ("ConnectionId");
                CREATE INDEX IF NOT EXISTS "IX_AtlasIndexJobs_Status"
                    ON "AtlasIndexJobs" ("Status");
                CREATE INDEX IF NOT EXISTS "IX_AtlasRequestHistory_ConnectionId"
                    ON "AtlasRequestHistory" ("ConnectionId");
                CREATE INDEX IF NOT EXISTS "IX_AtlasRequestHistory_CreatedAt"
                    ON "AtlasRequestHistory" ("CreatedAt");
                CREATE INDEX IF NOT EXISTS "IX_AtlasActivity_ConnectionId"
                    ON "AtlasActivity" ("ConnectionId");
                CREATE INDEX IF NOT EXISTS "IX_AtlasActivity_CreatedAt"
                    ON "AtlasActivity" ("CreatedAt");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
