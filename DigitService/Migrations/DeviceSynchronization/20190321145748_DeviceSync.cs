using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DigitService.Migrations.DeviceSynchronization
{
    public partial class DeviceSync : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sync_Devices",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    OwnerId = table.Column<string>(nullable: true),
                    UpToDate = table.Column<bool>(nullable: false),
                    LastSyncTime = table.Column<DateTime>(nullable: true),
                    FocusItemId = table.Column<string>(nullable: true),
                    FocusItemDigest = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sync_Devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sync_SyncActions",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    ActionId = table.Column<string>(nullable: false),
                    UserId = table.Column<string>(nullable: false),
                    RequestedFor = table.Column<DateTime>(nullable: false),
                    Done = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sync_SyncActions", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sync_Devices");

            migrationBuilder.DropTable(
                name: "Sync_SyncActions");
        }
    }
}
