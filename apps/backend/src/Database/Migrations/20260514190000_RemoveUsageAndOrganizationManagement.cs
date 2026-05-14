using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OffceOs.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUsageAndOrganizationManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IntegrationDeployments_Organizations_OrganizationId",
                table: "IntegrationDeployments");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Organizations_CurrentOrganizationId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Workspaces_Organizations_OrganizationId",
                table: "Workspaces");

            migrationBuilder.DropTable(name: "AccessGroupMembers");
            migrationBuilder.DropTable(name: "AccessGroupWorkspaceGrants");
            migrationBuilder.DropTable(name: "AgentUsageContextParts");
            migrationBuilder.DropTable(name: "OrganizationAuditLogs");
            migrationBuilder.DropTable(name: "OrganizationPolicyProfiles");
            migrationBuilder.DropTable(name: "OrgMembers");
            migrationBuilder.DropTable(name: "WorkspaceOrganizationGrants");
            migrationBuilder.DropTable(name: "AccessGroups");
            migrationBuilder.DropTable(name: "AgentUsageCalls");
            migrationBuilder.DropTable(name: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Workspaces_OrganizationId_Name",
                table: "Workspaces");

            migrationBuilder.DropIndex(
                name: "IX_Users_CurrentOrganizationId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationDeployments_OrganizationId",
                table: "IntegrationDeployments");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "CurrentOrganizationId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "IntegrationDeployments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Organization management and AgentUsage were removed in favor of workspace RBAC and ResourceLogs.");
        }
    }
}
