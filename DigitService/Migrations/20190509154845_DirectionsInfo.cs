using Microsoft.EntityFrameworkCore.Migrations;

namespace DigitService.Migrations
{
    public partial class DirectionsInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoredDirectionsInfo",
                columns: table => new
                {
                    FocusItemId = table.Column<string>(nullable: false),
                    DirectionsKey = table.Column<string>(nullable: true),
                    PreferredRoute = table.Column<int>(nullable: true),
                    DirectionsNotFound = table.Column<bool>(nullable: true),
                    PlaceNotFound = table.Column<bool>(nullable: true),
                    Lat = table.Column<double>(nullable: true),
                    Lng = table.Column<double>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredDirectionsInfo", x => x.FocusItemId);
                    table.ForeignKey(
                        name: "FK_StoredDirectionsInfo_FocusItems_FocusItemId",
                        column: x => x.FocusItemId,
                        principalTable: "FocusItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoredDirectionsInfo");
        }
    }
}
