#nullable disable

namespace OffceOs.Database.Migrations
{
    /// <inheritdoc />
    public partial class WorkspaceIntegrationCredentialVault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "IntegrationCredentials"
                WHERE "WorkspaceId" IS NULL;
                """);

            migrationBuilder.RenameColumn(
                name: "EncryptedCredentials",
                table: "IntegrationCredentials",
                newName: "EncryptedSecretEnvelope");

            migrationBuilder.AlterColumn<string>(
                name: "EncryptedSecretEnvelope",
                table: "IntegrationCredentials",
                type: "character varying(32768)",
                maxLength: 32768,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16384)",
                oldMaxLength: 16384);

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkspaceId",
                table: "IntegrationCredentials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "IntegrationCredentials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthKind",
                table: "IntegrationCredentials",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "api_key");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "IntegrationCredentials",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAtUtc",
                table: "IntegrationCredentials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedAt",
                table: "IntegrationCredentials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicAuthMetadataJson",
                table: "IntegrationCredentials",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScopesJson",
                table: "IntegrationCredentials",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "IntegrationCredentials",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "active");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "IntegrationCredentials",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidatedAt",
                table: "IntegrationCredentials",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "AuthKind",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "LastUsedAt",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "PublicAuthMetadataJson",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "ScopesJson",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "State",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "ValidatedAt",
                table: "IntegrationCredentials");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkspaceId",
                table: "IntegrationCredentials",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "EncryptedSecretEnvelope",
                table: "IntegrationCredentials",
                type: "character varying(16384)",
                maxLength: 16384,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32768)",
                oldMaxLength: 32768);

            migrationBuilder.RenameColumn(
                name: "EncryptedSecretEnvelope",
                table: "IntegrationCredentials",
                newName: "EncryptedCredentials");
        }
    }
}
