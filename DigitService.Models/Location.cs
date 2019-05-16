﻿using System;

namespace DigitService.Models
{
    public class LocationResponse
    {
        public DateTimeOffset? NextUpdateRequiredAt { get; set; }
        public GeofenceRequest[] Geofences { get; set; }
    }

    public class GeofenceRequest
    {
        public string Id { get; set; }
        public string FocusItemId { get; set; }
        public bool Exit { get; set; }
        public double Radius { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
    }
}
