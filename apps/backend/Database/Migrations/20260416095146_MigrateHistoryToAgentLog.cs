using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseAgentOs.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class MigrateHistoryToAgentLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Copy conversation rows into AgentLogs. Role → Type mapping:
            //   'user'      → MessageIn
            //   'assistant' → MessageOut
            //   anything else → System
            migrationBuilder.Sql(@"
                INSERT INTO ""AgentLogs""
                    (""Id"", ""AgentId"", ""Time"", ""Type"", ""Tool"", ""Integration"",
                     ""Channel"", ""Content"", ""DurationMs"", ""InputTokens"", ""OutputTokens"", ""CorrelationId"")
                SELECT
                    gen_random_uuid(),
                    c.""AgentId"",
                    c.""CreatedAt"",
                    CASE c.""Role""
                        WHEN 'user'      THEN 'MessageIn'
                        WHEN 'assistant' THEN 'MessageOut'
                        ELSE 'System'
                    END,
                    NULL,
                    NULL,
                    NULL,
                    c.""Content"",
                    NULL,
                    NULL,
                    NULL,
                    NULLIF(c.""SessionId"", '')
                FROM ""AgentConversations"" c;
            ");

            // Copy each tool-call row into TWO AgentLogs rows (ToolCall + ToolResult),
            // sharing CorrelationId = the original Id as text.
            migrationBuilder.Sql(@"
                INSERT INTO ""AgentLogs""
                    (""Id"", ""AgentId"", ""Time"", ""Type"", ""Tool"", ""Integration"",
                     ""Channel"", ""Content"", ""DurationMs"", ""InputTokens"", ""OutputTokens"", ""CorrelationId"")
                SELECT
                    gen_random_uuid(),
                    t.""AgentId"",
                    t.""Timestamp"",
                    'ToolCall',
                    t.""Action"",
                    t.""SkillName"",
                    NULL,
                    t.""ParamsJson"",
                    NULL,
                    NULL,
                    NULL,
                    t.""Id""::text
                FROM ""AgentToolCalls"" t
                UNION ALL
                SELECT
                    gen_random_uuid(),
                    t.""AgentId"",
                    t.""Timestamp"" + INTERVAL '1 millisecond',
                    'ToolResult',
                    t.""Action"",
                    t.""SkillName"",
                    NULL,
                    COALESCE(t.""ResultSummary"", ''),
                    CAST(LEAST(t.""DurationMs"", 2147483647) AS INT),
                    NULL,
                    NULL,
                    t.""Id""::text
                FROM ""AgentToolCalls"" t;
            ");

            migrationBuilder.DropTable(
                name: "AgentConversations");

            migrationBuilder.DropTable(
                name: "AgentToolCalls");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SessionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ToolCalls = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentToolCalls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    ParamsJson = table.Column<string>(type: "text", nullable: false),
                    ResultSummary = table.Column<string>(type: "text", nullable: true),
                    SkillName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentToolCalls", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentConversations_AgentId",
                table: "AgentConversations",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentConversations_AgentId_SessionId",
                table: "AgentConversations",
                columns: new[] { "AgentId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolCalls_AgentId",
                table: "AgentToolCalls",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolCalls_AgentId_Timestamp",
                table: "AgentToolCalls",
                columns: new[] { "AgentId", "Timestamp" });
        }
    }
}
