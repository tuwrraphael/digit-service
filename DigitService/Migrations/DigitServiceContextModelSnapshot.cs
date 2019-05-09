﻿// <auto-generated />
using System;
using DigitService.Impl.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DigitService.Migrations
{
    [DbContext(typeof(DigitServiceContext))]
    partial class DigitServiceContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.4-servicing-10062");

            modelBuilder.Entity("DigitService.Impl.EF.StoredBatteryMeasurement", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("DeviceId");

                    b.Property<DateTime>("MeasurementTime");

                    b.Property<uint>("RawValue");

                    b.HasKey("Id");

                    b.HasIndex("DeviceId");

                    b.ToTable("BatteryMeasurements");
                });

            modelBuilder.Entity("DigitService.Impl.EF.StoredCalendarEvent", b =>
                {
                    b.Property<string>("Id");

                    b.Property<string>("FeedId");

                    b.Property<string>("CalendarEventHash");

                    b.HasKey("Id", "FeedId");

                    b.ToTable("StoredCalendarEvent");
                });

            modelBuilder.Entity("DigitService.Impl.EF.StoredDevice", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<double>("BatteryCutOffVoltage");

                    b.Property<double>("BatteryMaxVoltage");

                    b.Property<double>("BatteryMeasurmentRange");

                    b.Property<string>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Devices");
                });

            modelBuilder.Entity("DigitService.Impl.EF.StoredDirectionsInfo", b =>
                {
                    b.Property<string>("FocusItemId");

                    b.Property<string>("DirectionsKey");

                    b.Property<bool?>("DirectionsNotFound");

                    b.Property<double?>("Lat");

                    b.Property<double?>("Lng");

                    b.Property<bool?>("PlaceNotFound");

                    b.Property<int?>("PreferredRoute");

                    b.HasKey("FocusItemId");

                    b.ToTable("StoredDirectionsInfo");
                });

            modelBuilder.Entity("DigitService.Impl.EF.StoredFocusItem", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("ActiveEnd");

                    b.Property<DateTime>("ActiveStart");

                    b.Property<string>("CalendarEventFeedId");

                    b.Property<string>("CalendarEventId");

                    b.Property<string>("DirectionsKey");

                    b.Property<DateTime>("IndicateAt");

                    b.Property<string>("UserId");

                    b.Property<bool>("UserNotified");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.HasIndex("CalendarEventId", "CalendarEventFeedId")
                        .IsUnique();

                    b.ToTable("FocusItems");
                });

            modelBuilder.Entity("DigitService.Impl.EF.StoredLocation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<double>("Accuracy");

                    b.Property<double>("Lat");

                    b.Property<double>("Lng");

                    b.Property<DateTime>("Timestamp");

                    b.HasKey("Id");

                    b.ToTable("StoredLocation");
                });

            modelBuilder.Entity("DigitService.Models.User", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ActiveFocusItem");

                    b.Property<DateTime?>("GeofenceFrom");

                    b.Property<DateTime?>("GeofenceTo");

                    b.Property<string>("ReminderId");

                    b.Property<int?>("StoredLocationId");

                    b.HasKey("Id");

                    b.HasIndex("StoredLocationId")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("DigitService.Impl.EF.StoredBatteryMeasurement", b =>
                {
                    b.HasOne("DigitService.Impl.EF.StoredDevice", "Device")
                        .WithMany("BatteryMeasurements")
                        .HasForeignKey("DeviceId");
                });

            modelBuilder.Entity("DigitService.Impl.EF.StoredDevice", b =>
                {
                    b.HasOne("DigitService.Models.User", "User")
                        .WithMany("Devices")
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("DigitService.Impl.EF.StoredDirectionsInfo", b =>
                {
                    b.HasOne("DigitService.Impl.EF.StoredFocusItem", "FocusItem")
                        .WithOne("Directions")
                        .HasForeignKey("DigitService.Impl.EF.StoredDirectionsInfo", "FocusItemId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("DigitService.Impl.EF.StoredFocusItem", b =>
                {
                    b.HasOne("DigitService.Models.User", "User")
                        .WithMany("FocusItems")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DigitService.Impl.EF.StoredCalendarEvent", "CalendarEvent")
                        .WithOne("FocusItem")
                        .HasForeignKey("DigitService.Impl.EF.StoredFocusItem", "CalendarEventId", "CalendarEventFeedId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("DigitService.Models.User", b =>
                {
                    b.HasOne("DigitService.Impl.EF.StoredLocation", "StoredLocation")
                        .WithOne("User")
                        .HasForeignKey("DigitService.Models.User", "StoredLocationId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
