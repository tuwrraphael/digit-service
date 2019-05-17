using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DigitService.Migrations
{
    public partial class MultiGeofence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TravelStatus",
                table: "StoredDirectionsInfo",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Geofences",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    FocusItemId = table.Column<string>(nullable: true),
                    Exit = table.Column<bool>(nullable: false),
                    Radius = table.Column<double>(nullable: false),
                    Lat = table.Column<double>(nullable: false),
                    Lng = table.Column<double>(nullable: false),
                    Start = table.Column<DateTime>(nullable: false),
                    End = table.Column<DateTime>(nullable: false),
                    Triggered = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Geofences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Geofences_FocusItems_FocusItemId",
                        column: x => x.FocusItemId,
                        principalTable: "FocusItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Geofences_FocusItemId",
                table: "Geofences",
                column: "FocusItemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Geofences");

            migrationBuilder.DropColumn(
                name: "TravelStatus",
                table: "StoredDirectionsInfo");
        }
    }
}
