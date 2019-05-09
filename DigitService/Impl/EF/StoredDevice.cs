using DigitService.Models;
using System;
using System.Collections.Generic;

namespace DigitService.Impl.EF
{
    public class StoredDevice
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public double BatteryCutOffVoltage { get; set; }
        public double BatteryMaxVoltage { get; set; }
        public double BatteryMeasurmentRange { get; set; }
        public List<StoredBatteryMeasurement> BatteryMeasurements { get; set; }
        public User User { get; internal set; }
    }

    public class StoredBatteryMeasurement
    {
        public string Id { get; set; }
        public string DeviceId { get; set; }
        public DateTime MeasurementTime { get; set; }
        public uint RawValue { get; set; }
        public StoredDevice Device { get; internal set; }
    }

    public class StoredLocation
    {
        public int Id { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double Accuracy { get; set; }
        public DateTime Timestamp { get; set; }
        public User User { get; set; }
    }

    public class StoredFocusItem
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public StoredCalendarEvent CalendarEvent { get; set; }
        public string CalendarEventId { get; set; }
        public string CalendarEventFeedId { get; set; }
        public bool UserNotified { get; set; }
        public DateTime ActiveEnd { get; set; }
        public DateTime ActiveStart { get; set; }
        public DateTime IndicateAt { get; set; }
        [Obsolete]
        public string DirectionsKey { get; set; }
        public StoredDirectionsInfo Directions { get; set; }
    }

    public class StoredDirectionsInfo
    {
        public string FocusItemId { get; set; }
        public StoredFocusItem FocusItem { get; set; }
        public string DirectionsKey { get; set; }
        public int? PreferredRoute { get; set; }
        public bool? DirectionsNotFound { get; set; }
        public bool? PlaceNotFound { get; set; }
        public double? Lat { get; set; }
        public double? Lng { get; set; }
    }

    public class StoredCalendarEvent
    {
        public string Id { get; set; }
        public string FeedId { get; set; }
        public string CalendarEventHash { get; set; }
        public StoredFocusItem FocusItem { get; internal set; }
    }
}
