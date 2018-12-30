using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DigitService.Migrations
{
    public partial class Focus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoredCalendarEvent",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    FeedId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredCalendarEvent", x => new { x.Id, x.FeedId });
                });

            migrationBuilder.CreateTable(
                name: "StoredLocation",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Lat = table.Column<double>(nullable: false),
                    Lng = table.Column<double>(nullable: false),
                    Accuracy = table.Column<double>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredLocation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    ReminderId = table.Column<string>(nullable: true),
                    StoredLocationId = table.Column<int>(nullable: true),
                    LocationRequestTime = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_StoredLocation_StoredLocationId",
                        column: x => x.StoredLocationId,
                        principalTable: "StoredLocation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    UserId = table.Column<string>(nullable: true),
                    BatteryCutOffVoltage = table.Column<double>(nullable: false),
                    BatteryMaxVoltage = table.Column<double>(nullable: false),
                    BatteryMeasurmentRange = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FocusItems",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    UserId = table.Column<string>(nullable: true),
                    CalendarEventId = table.Column<string>(nullable: true),
                    CalendarEventFeedId = table.Column<string>(nullable: true),
                    UserNotified = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FocusItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FocusItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FocusItems_StoredCalendarEvent_CalendarEventId_CalendarEventFeedId",
                        columns: x => new { x.CalendarEventId, x.CalendarEventFeedId },
                        principalTable: "StoredCalendarEvent",
                        principalColumns: new[] { "Id", "FeedId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BatteryMeasurements",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    DeviceId = table.Column<string>(nullable: true),
                    MeasurementTime = table.Column<DateTime>(nullable: false),
                    RawValue = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatteryMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatteryMeasurements_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BatteryMeasurements_DeviceId",
                table: "BatteryMeasurements",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_UserId",
                table: "Devices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FocusItems_UserId",
                table: "FocusItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FocusItems_CalendarEventId_CalendarEventFeedId",
                table: "FocusItems",
                columns: new[] { "CalendarEventId", "CalendarEventFeedId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_StoredLocationId",
                table: "Users",
                column: "StoredLocationId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BatteryMeasurements");

            migrationBuilder.DropTable(
                name: "FocusItems");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "StoredCalendarEvent");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "StoredLocation");
        }
    }
}
