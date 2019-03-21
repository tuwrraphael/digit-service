using System;

namespace DigitService.Impl
{
    internal static class FocusConstants
    {
        internal static readonly TimeSpan FocusScanTime = new TimeSpan(2, 0, 0);
        internal static readonly TimeSpan CalendarServiceInacurracy = new TimeSpan(0, 10, 0);

        internal static readonly TimeSpan LastLocationCacheTime = new TimeSpan(0, 5, 0);
        internal static readonly TimeSpan LocationRequestExpectedTime = new TimeSpan(0, 5, 0);

        internal static readonly TimeSpan NoUpdateBeforeDepartureMargin = new TimeSpan(0, 10, 0);

        internal const uint GeofenceRadius = 50;

        internal const double GeofenceThreshold = 0.95;

        internal static readonly TimeSpan DefaultTravelTime = new TimeSpan(0, 45, 0);
        internal static readonly TimeSpan NotifyTime = new TimeSpan(0, 6, 0);
        internal static readonly TimeSpan ButlerInaccuracy = new TimeSpan(0, 1, 0);

        internal static readonly TimeSpan ItemActiveBeforeIndicate = new TimeSpan(0, 30, 0);
    }
}
