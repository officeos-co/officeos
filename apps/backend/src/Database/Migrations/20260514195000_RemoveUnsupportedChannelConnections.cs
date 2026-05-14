#nullable disable

namespace OffceOs.Database.Migrations;

[DbContext(typeof(EaosDbContext))]
[Migration("20260514195000_RemoveUnsupportedChannelConnections")]
public partial class RemoveUnsupportedChannelConnections : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM "ChannelConnections"
            WHERE "ChannelType" NOT IN ('internal', 'slack', 'telegram');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Unsupported channel connection types were removed.");
    }
}
