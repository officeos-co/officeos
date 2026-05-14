#nullable disable

namespace OffceOs.Database.Migrations
{
    /// <inheritdoc />
    public partial class OrgManagedAdminRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessGroups_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationDeployments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationDeployments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationDeployments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntegrationDeployments_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationDeployments_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationPolicyProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrowserToolsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    NetworkToolsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ShellToolsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    FileWriteToolsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AllowedToolsJson = table.Column<string>(type: "jsonb", nullable: false),
                    DeniedToolsJson = table.Column<string>(type: "jsonb", nullable: false),
                    AllowedIntegrationsJson = table.Column<string>(type: "jsonb", nullable: false),
                    DeniedIntegrationsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationPolicyProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationPolicyProfiles_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationProviderProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AllowedModelsJson = table.Column<string>(type: "jsonb", nullable: false),
                    EncryptedApiKey = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    ConfiguredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationProviderProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationProviderProfiles_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccessGroupMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessGroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessGroupMembers_AccessGroups_AccessGroupId",
                        column: x => x.AccessGroupId,
                        principalTable: "AccessGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessGroupMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccessGroupWorkspaceGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessGroupWorkspaceGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessGroupWorkspaceGrants_AccessGroups_AccessGroupId",
                        column: x => x.AccessGroupId,
                        principalTable: "AccessGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessGroupWorkspaceGrants_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessGroupMembers_AccessGroupId_UserId",
                table: "AccessGroupMembers",
                columns: new[] { "AccessGroupId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessGroupMembers_UserId",
                table: "AccessGroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGroups_OrganizationId_Name",
                table: "AccessGroups",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessGroupWorkspaceGrants_AccessGroupId_WorkspaceId",
                table: "AccessGroupWorkspaceGrants",
                columns: new[] { "AccessGroupId", "WorkspaceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessGroupWorkspaceGrants_WorkspaceId",
                table: "AccessGroupWorkspaceGrants",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDeployments_CreatedById",
                table: "IntegrationDeployments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDeployments_OrganizationId",
                table: "IntegrationDeployments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDeployments_WorkspaceId_IntegrationName",
                table: "IntegrationDeployments",
                columns: new[] { "WorkspaceId", "IntegrationName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationPolicyProfiles_OrganizationId",
                table: "OrganizationPolicyProfiles",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationProviderProfiles_OrganizationId_Provider",
                table: "OrganizationProviderProfiles",
                columns: new[] { "OrganizationId", "Provider" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessGroupMembers");

            migrationBuilder.DropTable(
                name: "AccessGroupWorkspaceGrants");

            migrationBuilder.DropTable(
                name: "IntegrationDeployments");

            migrationBuilder.DropTable(
                name: "OrganizationPolicyProfiles");

            migrationBuilder.DropTable(
                name: "OrganizationProviderProfiles");

            migrationBuilder.DropTable(
                name: "AccessGroups");
        }
    }
}
