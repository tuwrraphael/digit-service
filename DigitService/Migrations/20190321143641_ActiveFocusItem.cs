using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DigitService.Migrations
{
    public partial class ActiveFocusItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActiveFocusItem",
                table: "Users",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveFocusItem",
                table: "Users");

            migrationBuilder.AddColumn<DateTime>(
                name: "LocationRequestTime",
                table: "Users",
                nullable: true);
        }
    }
}
