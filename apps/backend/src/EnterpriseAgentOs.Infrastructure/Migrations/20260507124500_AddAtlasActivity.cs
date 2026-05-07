using EnterpriseAgentOs.Infrastructure.Common;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseAgentOs.Infrastructure.Migrations
{
    [DbContext(typeof(EaosDbContext))]
    [Migration("20260507124500_AddAtlasActivity")]
    public partial class AddAtlasActivity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
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

                CREATE INDEX IF NOT EXISTS "IX_AtlasActivity_ConnectionId"
                    ON "AtlasActivity" ("ConnectionId");
                CREATE INDEX IF NOT EXISTS "IX_AtlasActivity_CreatedAt"
                    ON "AtlasActivity" ("CreatedAt");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AtlasActivity");
        }
    }
}
