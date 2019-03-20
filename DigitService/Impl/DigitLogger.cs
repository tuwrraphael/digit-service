using Digit.Abstractions.Models;
using Digit.Abstractions.Service;
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
        private readonly ILogBackend logBackend;

        public DigitLogger(ILogBackend logBackend)
        {
            this.logBackend = logBackend;
        }

        public async Task Log(string user, string message, int code = 0)
        {
            //TODO replace with user id
            await logBackend.LogAsync("12345", new LogEntry()
            {
                Author = "DigitService",
                Code = code,
                OccurenceTime = DateTime.Now,
                Message = message
            });
        }

        public async Task Log(string user, LogEntry entry)
        {
            await logBackend.LogAsync(user, entry);
        }
    }
}
