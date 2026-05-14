using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OffceOs.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentOrganizationContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Users"
                ADD COLUMN IF NOT EXISTS "CurrentOrganizationId" uuid;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Organizations"
                ADD COLUMN IF NOT EXISTS "Kind" character varying(32) NOT NULL DEFAULT 'individual';
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_Users_CurrentOrganizationId"
                ON "Users" ("CurrentOrganizationId");
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_Users_Organizations_CurrentOrganizationId'
                    ) THEN
                        ALTER TABLE "Users"
                        ADD CONSTRAINT "FK_Users_Organizations_CurrentOrganizationId"
                        FOREIGN KEY ("CurrentOrganizationId")
                        REFERENCES "Organizations" ("Id")
                        ON DELETE SET NULL;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Users"
                DROP CONSTRAINT IF EXISTS "FK_Users_Organizations_CurrentOrganizationId";
                """);

            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_Users_CurrentOrganizationId";
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Users"
                DROP COLUMN IF EXISTS "CurrentOrganizationId";
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Organizations"
                DROP COLUMN IF EXISTS "Kind";
                """);
        }
    }
}
