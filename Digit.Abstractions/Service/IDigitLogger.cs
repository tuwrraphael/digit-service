using Digit.Abstractions.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Digit.Abstractions.Service
{

    public interface IDigitLogger
    {
        Task Log(string user, LogEntry entry);
        Task LogForUser(string userId, string message,
            DigitTraceAction action = DigitTraceAction.Default,
            IDictionary<string, object> additionalData = null,
            LogLevel logLevel = LogLevel.Information);
        Task LogForFocusItem(string userId, string focusItemId, string message,
            DigitTraceAction action = DigitTraceAction.Default,
            IDictionary<string, object> additionalData = null,
            LogLevel logLevel = LogLevel.Information);
    }

    public static class IDigitLoggerExtensions
    {
        public static Task LogErrorForUser(this IDigitLogger digitLogger, string userId, string message,
            DigitTraceAction action = DigitTraceAction.Default,
            IDictionary<string, object> additionalData = null)
        {
            return digitLogger.LogForUser(userId, message, action, additionalData, LogLevel.Error);
        }
    }

    public enum DigitTraceAction
    {
        Default = 0,
        CreatedAccount = 1,
        RequestPush = 2,
        TraceOnRoute = 3,
    }
}

