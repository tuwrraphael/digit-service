using DigitService.Models;
using DigitService.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Impl
{
    public class DigitLogger : IDigitLogger
    {
        private readonly IDeviceRepository deviceRepository;

        public DigitLogger(IDeviceRepository deviceRepository)
        {
            this.deviceRepository = deviceRepository;
        }

        public async Task Log(string user, string message, int code = 0)
        {
            //TODO replace with user id
            await deviceRepository.LogAsync("12345", new LogEntry()
            {
                Author = "DigitService",
                Code = code,
                OccurenceTime = DateTime.Now,
                Message = message
            });
        }
    }
}
