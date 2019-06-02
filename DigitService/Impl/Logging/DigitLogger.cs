using Digit.Abstractions.Models;
using Digit.Abstractions.Service;
using DigitService.Service;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Impl.Logging
{
    public class DigitLogger : IDigitLogger
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IEnumerable<IRealtimeLogSubscriber> _subscribers;

        public DigitLogger(TelemetryClient telemetryClient,
            IEnumerable<IRealtimeLogSubscriber> subscribers)
        {
            _telemetryClient = telemetryClient;
            _subscribers = subscribers;
        }

        public async Task Log(string userId, LegacyLogRequest entry)
        {
            var telemetry = new TraceTelemetry(entry.Message, FromEntry(entry));
            var id = Guid.NewGuid().ToString();
            telemetry.Properties.Add("digitTraceId", id);
            telemetry.Context.Cloud.RoleName = entry.Author;
            telemetry.Context.User.Id = userId;
            _telemetryClient.TrackTrace(telemetry);
            var e = new LogEntry()
            {
                UserId = userId,
                AdditionalData = null,
                Author = entry.Author,
                DigitTraceAction = DigitTraceAction.Default,
                FocusItemId = null,
                Id = id,
                LogLevel = LogLevelFromEntry(entry),
                Message = entry.Message,
                Timestamp = DateTimeOffset.Now
            };
            await Task.WhenAll(_subscribers.Select(s => s.Add(e)));
        }

        public async Task LogForFocusItem(string userId, string focusItemId, string message, DigitTraceAction action = DigitTraceAction.Default, IDictionary<string, object> additionalData = null, LogLevel logLevel = LogLevel.Information)
        {
            if (null == additionalData)
            {
                additionalData = new Dictionary<string, object>();
            }
            additionalData.Add("focusItemId", focusItemId);
            await LogForUser(userId, message, action, additionalData, logLevel);
        }

        public async Task LogForUser(string userId, string message, DigitTraceAction action = DigitTraceAction.Default, IDictionary<string, object> additionalData = null, LogLevel logLevel = LogLevel.Information)
        {
            var telemetry = new TraceTelemetry(message, FromLogLevel(logLevel));
            if (null != additionalData)
            {
                foreach (var prop in additionalData)
                {
                    telemetry.Properties.Add(prop.Key, prop.Value.ToString());
                }
            }
            telemetry.Properties.Add("digitTraceAction", action.ToString());
            telemetry.Context.User.Id = userId;
            string id = Guid.NewGuid().ToString();
            telemetry.Properties.Add("digitTraceId", id);
            _telemetryClient.TrackTrace(telemetry);
            var e = new LogEntry()
            {
                UserId = userId,
                AdditionalData = telemetry.Properties,
                Author = "digit-svc",
                DigitTraceAction = action,
                FocusItemId = telemetry.Properties.ContainsKey("focusItemId") ? telemetry.Properties["focusItemId"] : null,
                Id = id,
                LogLevel = logLevel,
                Message = message,
                Timestamp = DateTimeOffset.Now
            };
            await Task.WhenAll(_subscribers.Select(s => s.Add(e)));
        }

        private SeverityLevel FromLogLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Information:
                    return SeverityLevel.Information;
                case LogLevel.Error:
                    return SeverityLevel.Error;
            }
            return SeverityLevel.Information;
        }

        private SeverityLevel FromEntry(LegacyLogRequest logLevel)
        {
            switch (logLevel.Code)
            {
                case 0:
                case 1:
                    return SeverityLevel.Information;
                case 3:
                    return SeverityLevel.Error;
            }
            return SeverityLevel.Information;
        }

        private LogLevel LogLevelFromEntry(LegacyLogRequest logLevel)
        {
            switch (logLevel.Code)
            {
                case 0:
                case 1:
                    return LogLevel.Information;
                case 3:
                    return LogLevel.Error;
            }
            return LogLevel.Information;
        }
    }
}
