#nullable disable

namespace EnterpriseAgentOs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FlattenManifestAndDropUnusedTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RunnerJobs");

            migrationBuilder.DropTable(
                name: "SkillRegistry");

            migrationBuilder.DropTable(
                name: "Runners");

            migrationBuilder.DropColumn(
                name: "ManifestJson",
                table: "Skills");

            migrationBuilder.AddColumn<string>(
                name: "ActionsJson",
                table: "Skills",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorName",
                table: "Skills",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorUrl",
                table: "Skills",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "Categories",
                table: "Skills",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Skills",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Changelog",
                table: "Skills",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContributorsJson",
                table: "Skills",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CredentialFieldsJson",
                table: "Skills",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "Keywords",
                table: "Skills",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "License",
                table: "Skills",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Logo",
                table: "Skills",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Readme",
                table: "Skills",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Repository",
                table: "Skills",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresApproval",
                table: "Skills",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActionsJson",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "AuthorName",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "AuthorUrl",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "Categories",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "Changelog",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "ContributorsJson",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "CredentialFieldsJson",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "Keywords",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "License",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "Logo",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "Readme",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "Repository",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "RequiresApproval",
                table: "Skills");

            migrationBuilder.AddColumn<string>(
                name: "ManifestJson",
                table: "Skills",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Runners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastHeartbeatAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RegistrationTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Runners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Runners_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillRegistry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublishedById = table.Column<Guid>(type: "uuid", nullable: true),
                    BundleUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ManifestJson = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NpmPackage = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillRegistry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillRegistry_Users_PublishedById",
                        column: x => x.PublishedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RunnerJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Result = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunnerJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunnerJobs_Runners_RunnerId",
                        column: x => x.RunnerId,
                        principalTable: "Runners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerJobs_RunnerId_Status",
                table: "RunnerJobs",
                columns: new[] { "RunnerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Runners_OwnerId",
                table: "Runners",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillRegistry_Name",
                table: "SkillRegistry",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillRegistry_PublishedById",
                table: "SkillRegistry",
                column: "PublishedById");
        }
    }
}
