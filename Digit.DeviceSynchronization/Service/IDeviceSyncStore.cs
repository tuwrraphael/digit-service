using Digit.DeviceSynchronization.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Service
{
    internal interface IDeviceSyncStore
    {
        Task CreateAsync(string userId, string deviceId, DeviceSyncRequest request);
        Task<Device[]> GetForUserAsync(string userId);
    }
}
