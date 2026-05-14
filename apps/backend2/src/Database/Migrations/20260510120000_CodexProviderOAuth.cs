namespace OffceOs.Database.Migrations;

public partial class CodexProviderOAuth : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "EncryptedAccessToken",
            table: "OAuthTokens",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(16384)",
            oldMaxLength: 16384,
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "EncryptedAccessToken",
            table: "OAuthTokens",
            type: "character varying(16384)",
            maxLength: 16384,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);
    }
}
