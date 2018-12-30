using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DigitService.Migrations
{
    public partial class FocusItemExtension : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActiveEnd",
                table: "FocusItems",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "DirectionsKey",
                table: "FocusItems",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IndicateAt",
                table: "FocusItems",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveEnd",
                table: "FocusItems");

            migrationBuilder.DropColumn(
                name: "DirectionsKey",
                table: "FocusItems");

            migrationBuilder.DropColumn(
                name: "IndicateAt",
                table: "FocusItems");
        }
    }
}
