#nullable disable

namespace OffceOs.Database.Migrations;

[DbContext(typeof(EaosDbContext))]
[Migration("20260514194500_RemoveNonPersonalWorkspaces")]
public partial class RemoveNonPersonalWorkspaces : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE "Users"
            SET "CurrentWorkspaceId" = NULL
            WHERE "CurrentWorkspaceId" IN (
                SELECT "Id"
                FROM "Workspaces"
                WHERE "OwnerKind" IS DISTINCT FROM 'personal'
            );

            DELETE FROM "Workspaces"
            WHERE "OwnerKind" IS DISTINCT FROM 'personal';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Non-personal workspaces were removed with organization management.");
    }
}
