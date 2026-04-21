#nullable disable

namespace EnterpriseAgentOs.Infrastructure.Common.Migrations
{
    /// <inheritdoc />
    public partial class DropVaultAndAgentMemory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentCacheEntries");

            migrationBuilder.DropTable(
                name: "AgentMemories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentCacheEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CacheKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Response = table.Column<string>(type: "text", nullable: false),
                    TokenCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentCacheEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentMemories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Importance = table.Column<double>(type: "double precision", nullable: true),
                    Key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Namespace = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, defaultValue: "default"),
                    SessionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SupersededBy = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentMemories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentCacheEntries_AccessedAt",
                table: "AgentCacheEntries",
                column: "AccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AgentCacheEntries_AgentId_CacheKey",
                table: "AgentCacheEntries",
                columns: new[] { "AgentId", "CacheKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentMemories_AgentId",
                table: "AgentMemories",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentMemories_AgentId_Category",
                table: "AgentMemories",
                columns: new[] { "AgentId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentMemories_AgentId_Key",
                table: "AgentMemories",
                columns: new[] { "AgentId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentMemories_AgentId_SessionId",
                table: "AgentMemories",
                columns: new[] { "AgentId", "SessionId" });
        }
    }
}
