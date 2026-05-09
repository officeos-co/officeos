#nullable disable

namespace OffceOs.Database.Migrations
{
    /// <inheritdoc />
    public partial class WorkspaceMembershipAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Integrations_OwnerId_WorkspaceId_Name",
                table: "Integrations");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationCredentials_OwnerId_WorkspaceId_IntegrationName",
                table: "IntegrationCredentials");

            migrationBuilder.DropForeignKey(
                name: "FK_Workspaces_Users_UserId",
                table: "Workspaces");

            migrationBuilder.DropIndex(
                name: "IX_Workspaces_UserId_Name",
                table: "Workspaces");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Workspaces",
                newName: "OwnerUserId");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Workspaces",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Workspaces",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerKind",
                table: "Workspaces",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "personal");

            migrationBuilder.Sql(
                """
                UPDATE "Workspaces"
                SET "OwnerKind" = 'personal';

                WITH ranked AS (
                    SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "OwnerUserId" ORDER BY "CreatedAt", "Id") AS rn
                    FROM "Workspaces"
                    WHERE "OwnerUserId" IS NOT NULL
                )
                UPDATE "Workspaces" w
                SET "IsDefault" = ranked.rn = 1
                FROM ranked
                WHERE w."Id" = ranked."Id";
                """);

            migrationBuilder.CreateTable(
                name: "WorkspaceMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkspaceMembers_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceOrganizationGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaxRole = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceOrganizationGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceOrganizationGrants_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkspaceOrganizationGrants_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "WorkspaceMembers" ("Id", "WorkspaceId", "UserId", "Role", "CreatedAt")
                SELECT
                    (substr(h, 1, 8) || '-' || substr(h, 9, 4) || '-' || substr(h, 13, 4) || '-' || substr(h, 17, 4) || '-' || substr(h, 21, 12))::uuid,
                    "Id",
                    "OwnerUserId",
                    'Owner',
                    NOW()
                FROM (
                    SELECT "Id", "OwnerUserId", md5("Id"::text || ':' || "OwnerUserId"::text || ':workspace-owner') AS h
                    FROM "Workspaces"
                    WHERE "OwnerUserId" IS NOT NULL
                ) AS memberships
                ON CONFLICT DO NOTHING;

                INSERT INTO "Workspaces" ("Id", "OwnerUserId", "OrganizationId", "OwnerKind", "Name", "IsDefault", "CreatedAt", "UpdatedAt")
                SELECT
                    (substr(h, 1, 8) || '-' || substr(h, 9, 4) || '-' || substr(h, 13, 4) || '-' || substr(h, 17, 4) || '-' || substr(h, 21, 12))::uuid,
                    NULL,
                    "Id",
                    'organization',
                    "Name",
                    TRUE,
                    NOW(),
                    NOW()
                FROM (
                    SELECT o."Id", o."Name", md5(o."Id"::text || ':organization-default-workspace') AS h
                    FROM "Organizations" o
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM "Workspaces" w
                        WHERE w."OrganizationId" = o."Id" AND w."IsDefault" = TRUE
                    )
                ) AS defaults;

                INSERT INTO "WorkspaceMembers" ("Id", "WorkspaceId", "UserId", "Role", "CreatedAt")
                SELECT
                    (substr(h, 1, 8) || '-' || substr(h, 9, 4) || '-' || substr(h, 13, 4) || '-' || substr(h, 17, 4) || '-' || substr(h, 21, 12))::uuid,
                    "WorkspaceId",
                    "OwnerUserId",
                    'Owner',
                    NOW()
                FROM (
                    SELECT w."Id" AS "WorkspaceId", o."OwnerUserId", md5(w."Id"::text || ':' || o."OwnerUserId"::text || ':org-workspace-owner') AS h
                    FROM "Workspaces" w
                    JOIN "Organizations" o ON o."Id" = w."OrganizationId"
                    WHERE w."OwnerKind" = 'organization'
                ) AS owner_members
                ON CONFLICT DO NOTHING;

                INSERT INTO "WorkspaceMembers" ("Id", "WorkspaceId", "UserId", "Role", "CreatedAt")
                SELECT
                    (substr(h, 1, 8) || '-' || substr(h, 9, 4) || '-' || substr(h, 13, 4) || '-' || substr(h, 17, 4) || '-' || substr(h, 21, 12))::uuid,
                    "WorkspaceId",
                    "UserId",
                    'Editor',
                    NOW()
                FROM (
                    SELECT w."Id" AS "WorkspaceId", m."UserId", md5(w."Id"::text || ':' || m."UserId"::text || ':org-default-editor') AS h
                    FROM "Workspaces" w
                    JOIN "OrgMembers" m ON m."OrganizationId" = w."OrganizationId"
                    WHERE w."OwnerKind" = 'organization'
                        AND w."IsDefault" = TRUE
                        AND m."Status" = 'active'
                        AND m."UserId" IS NOT NULL
                        AND m."Role" <> 'Owner'
                ) AS member_rows
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_OrganizationId_Name",
                table: "Workspaces",
                columns: new[] { "OrganizationId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_OwnerKind",
                table: "Workspaces",
                column: "OwnerKind");

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_OwnerUserId_Name",
                table: "Workspaces",
                columns: new[] { "OwnerUserId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_WorkspaceId_Name",
                table: "Integrations",
                columns: new[] { "WorkspaceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationCredentials_WorkspaceId_IntegrationName",
                table: "IntegrationCredentials",
                columns: new[] { "WorkspaceId", "IntegrationName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMembers_UserId",
                table: "WorkspaceMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMembers_WorkspaceId_UserId",
                table: "WorkspaceMembers",
                columns: new[] { "WorkspaceId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceOrganizationGrants_OrganizationId",
                table: "WorkspaceOrganizationGrants",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceOrganizationGrants_WorkspaceId_OrganizationId",
                table: "WorkspaceOrganizationGrants",
                columns: new[] { "WorkspaceId", "OrganizationId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Workspaces_Organizations_OrganizationId",
                table: "Workspaces",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Workspaces_Users_OwnerUserId",
                table: "Workspaces",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Integrations_WorkspaceId_Name",
                table: "Integrations");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationCredentials_WorkspaceId_IntegrationName",
                table: "IntegrationCredentials");

            migrationBuilder.DropForeignKey(
                name: "FK_Workspaces_Organizations_OrganizationId",
                table: "Workspaces");

            migrationBuilder.DropForeignKey(
                name: "FK_Workspaces_Users_OwnerUserId",
                table: "Workspaces");

            migrationBuilder.DropTable(
                name: "WorkspaceOrganizationGrants");

            migrationBuilder.DropTable(
                name: "WorkspaceMembers");

            migrationBuilder.DropIndex(
                name: "IX_Workspaces_OrganizationId_Name",
                table: "Workspaces");

            migrationBuilder.DropIndex(
                name: "IX_Workspaces_OwnerKind",
                table: "Workspaces");

            migrationBuilder.DropIndex(
                name: "IX_Workspaces_OwnerUserId_Name",
                table: "Workspaces");

            migrationBuilder.Sql(
                """
                DELETE FROM "Workspaces"
                WHERE "OwnerKind" = 'organization';
                """);

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "OwnerKind",
                table: "Workspaces");

            migrationBuilder.RenameColumn(
                name: "OwnerUserId",
                table: "Workspaces",
                newName: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_UserId_Name",
                table: "Workspaces",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_OwnerId_WorkspaceId_Name",
                table: "Integrations",
                columns: new[] { "OwnerId", "WorkspaceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationCredentials_OwnerId_WorkspaceId_IntegrationName",
                table: "IntegrationCredentials",
                columns: new[] { "OwnerId", "WorkspaceId", "IntegrationName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Workspaces_Users_UserId",
                table: "Workspaces",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
