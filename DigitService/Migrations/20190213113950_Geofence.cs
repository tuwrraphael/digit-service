using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DigitService.Migrations
{
    public partial class Geofence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GeofenceFrom",
                table: "Users",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GeofenceTo",
                table: "Users",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CalendarEventHash",
                table: "StoredCalendarEvent",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeofenceFrom",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GeofenceTo",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CalendarEventHash",
                table: "StoredCalendarEvent");
        }
    }
}
