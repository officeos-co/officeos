using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseAgentOs.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentMemoryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: all tables (AgentCacheEntries, AgentConversations, AgentMemories, DeviceCodes)
            // were already created in the preceding AddDeviceCodes migration.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: tables are dropped by the AddDeviceCodes Down method.
        }
    }
}
