using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DigitService.Migrations
{
    public partial class ActiveStart : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActiveStart",
                table: "FocusItems",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveStart",
                table: "FocusItems");
        }
    }
}
