#nullable disable

namespace OffceOs.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentUsageCalls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    RunId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false),
                    InputTokens = table.Column<long>(type: "bigint", nullable: false),
                    OutputTokens = table.Column<long>(type: "bigint", nullable: false),
                    CacheReadTokens = table.Column<long>(type: "bigint", nullable: true),
                    CacheWriteTokens = table.Column<long>(type: "bigint", nullable: true),
                    ReasoningTokens = table.Column<long>(type: "bigint", nullable: true),
                    EstimatedTokens = table.Column<bool>(type: "boolean", nullable: false),
                    Credits = table.Column<long>(type: "bigint", nullable: false),
                    Activity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentUsageCalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentUsageCalls_AgentRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "AgentRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AgentUsageCalls_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentUsageCalls_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentUsageCalls_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentUsageContextParts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CallId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Tool = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Integration = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Tokens = table.Column<long>(type: "bigint", nullable: false),
                    EstimatedTokens = table.Column<bool>(type: "boolean", nullable: false),
                    CharacterCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentUsageContextParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentUsageContextParts_AgentUsageCalls_CallId",
                        column: x => x.CallId,
                        principalTable: "AgentUsageCalls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentUsageCalls_AgentId",
                table: "AgentUsageCalls",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentUsageCalls_CorrelationId",
                table: "AgentUsageCalls",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentUsageCalls_OwnerId",
                table: "AgentUsageCalls",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentUsageCalls_OwnerId_Model_Time",
                table: "AgentUsageCalls",
                columns: new[] { "OwnerId", "Model", "Time" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentUsageCalls_OwnerId_Time",
                table: "AgentUsageCalls",
                columns: new[] { "OwnerId", "Time" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentUsageCalls_RunId",
                table: "AgentUsageCalls",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentUsageCalls_WorkspaceId",
                table: "AgentUsageCalls",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentUsageContextParts_CallId",
                table: "AgentUsageContextParts",
                column: "CallId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentUsageContextParts_Kind",
                table: "AgentUsageContextParts",
                column: "Kind");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentUsageContextParts");

            migrationBuilder.DropTable(
                name: "AgentUsageCalls");
        }
    }
}
