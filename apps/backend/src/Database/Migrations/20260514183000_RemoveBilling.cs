#nullable disable

namespace OffceOs.Database.Migrations;

public partial class RemoveBilling : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "OrgSubscriptions");
        migrationBuilder.DropTable(name: "UserSubscriptions");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
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
            constraints: table => table.PrimaryKey("PK_OrgSubscriptions", x => x.Id));

        migrationBuilder.CreateTable(
            name: "UserSubscriptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Plan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                BillingCycle = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
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
            constraints: table => table.PrimaryKey("PK_UserSubscriptions", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_OrgSubscriptions_OrganizationId",
            table: "OrgSubscriptions",
            column: "OrganizationId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserSubscriptions_UserId",
            table: "UserSubscriptions",
            column: "UserId",
            unique: true);
    }
}
