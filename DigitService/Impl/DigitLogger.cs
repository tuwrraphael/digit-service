using Digit.Abstractions.Models;
using Digit.Abstractions.Service;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitService.Impl
{
    public class DigitLogger : IDigitLogger
    {
        private readonly TelemetryClient _telemetryClient;

        public DigitLogger(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public async Task Log(string userId, LogEntry entry)
        {
            var telemetry = new TraceTelemetry(entry.Message, FromEntry(entry));
            telemetry.Context.Cloud.RoleName = entry.Author;
            telemetry.Context.User.Id = userId;
            _telemetryClient.TrackTrace(telemetry);
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
            _telemetryClient.TrackTrace(telemetry);
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

        private SeverityLevel FromEntry(LogEntry logLevel)
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
    }
}
