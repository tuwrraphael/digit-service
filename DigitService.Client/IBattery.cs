﻿using DigitService.Models;
using System.Threading.Tasks;

namespace DigitService.Client
{
    public interface IBattery
    {
        Task AddMeasurement(BatteryMeasurement measurement);
    }
}
