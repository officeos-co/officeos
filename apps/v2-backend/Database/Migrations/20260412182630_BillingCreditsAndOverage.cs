using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseAgentOs.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class BillingCreditsAndOverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TokensUsedThisMonth",
                table: "UserSubscriptions",
                newName: "CreditsUsedThisMonth");

            migrationBuilder.RenameColumn(
                name: "TokenBudgetPerMonth",
                table: "UserSubscriptions",
                newName: "CreditBudgetPerMonth");

            migrationBuilder.AddColumn<bool>(
                name: "OverageEnabled",
                table: "UserSubscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StripeOverageItemId",
                table: "UserSubscriptions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Agents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrgSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Plan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StripeCustomerId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    StripeOverageItemId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrentAgentLimit = table.Column<int>(type: "integer", nullable: false),
                    CreditBudgetPerMonth = table.Column<long>(type: "bigint", nullable: false),
                    CreditsUsedThisMonth = table.Column<long>(type: "bigint", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    OverageEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrgSubscriptions_OrganizationId",
                table: "OrgSubscriptions",
                column: "OrganizationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrgSubscriptions");

            migrationBuilder.DropColumn(
                name: "OverageEnabled",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "StripeOverageItemId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Agents");

            migrationBuilder.RenameColumn(
                name: "CreditsUsedThisMonth",
                table: "UserSubscriptions",
                newName: "TokensUsedThisMonth");

            migrationBuilder.RenameColumn(
                name: "CreditBudgetPerMonth",
                table: "UserSubscriptions",
                newName: "TokenBudgetPerMonth");
        }
    }
}
