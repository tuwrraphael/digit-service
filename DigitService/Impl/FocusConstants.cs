using System;

namespace DigitService.Controllers
{
    internal static class FocusConstants
    {
        internal static readonly TimeSpan FocusScanTime = new TimeSpan(2, 0, 0);
        internal static readonly TimeSpan CalendarServiceInacurracy = new TimeSpan(0, 10, 0);

        internal static readonly TimeSpan LastLocationCacheTime = new TimeSpan(0, 5, 0);
        internal static readonly TimeSpan LocationRequestInvalidationTime = new TimeSpan(0, 20, 0);

        internal static readonly TimeSpan NoUpdateBeforeDepartureMargin = new TimeSpan(0, 10, 0);

        internal const uint GeofenceRadius = 50;

        internal const double GeofenceThreshold = 0.95;
    }
}
