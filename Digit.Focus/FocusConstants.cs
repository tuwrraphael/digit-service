using System;

namespace Digit.Focus
{
    public static class FocusConstants
    {
        public static readonly TimeSpan FocusScanTime = new TimeSpan(2, 0, 0);
        public static readonly TimeSpan CalendarServiceInacurracy = new TimeSpan(0, 10, 0);

        public static readonly TimeSpan LastLocationCacheTime = new TimeSpan(0, 5, 0);
        public static readonly TimeSpan LocationRequestExpectedTime = new TimeSpan(0, 5, 0);

        public static readonly TimeSpan NoUpdateBeforeDepartureMargin = new TimeSpan(0, 10, 0);

        public const uint GeofenceRadius = 50;

        public const double GeofenceThreshold = 0.95;

        public static readonly TimeSpan DefaultTravelTime = new TimeSpan(0, 45, 0);
        public static readonly TimeSpan NotifyTime = new TimeSpan(0, 6, 0);
        public static readonly TimeSpan ButlerInaccuracy = new TimeSpan(0, 1, 0);

        public static readonly TimeSpan ItemActiveBeforeIndicateAlone = new TimeSpan(0, 30, 0);
        public static readonly TimeSpan ItemActiveBeforeIndicateMultiple = new TimeSpan(0, 10, 0);
    }
}
