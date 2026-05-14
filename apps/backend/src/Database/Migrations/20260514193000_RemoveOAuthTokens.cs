#nullable disable

namespace OffceOs.Database.Migrations;

public partial class RemoveOAuthTokens : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "OAuthGrantedScopes");
        migrationBuilder.DropTable(name: "OAuthTokens");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("OAuth token storage was removed with browser OAuth support.");
    }
}
