using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Digit.Abstractions.Models;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DigitService.Impl.Logging
{
    public class ApplicationInsightsLogReader
    {
        private readonly HttpClient _httpClient;

        public ApplicationInsightsLogReader(HttpClient client)
        {
            _httpClient = client;
        }

        public async Task<LogEntry[]> GetUserAILog(string applicationId, string userId, DateTimeOffset since)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["timespan"] = since.ToString("o");
            query["query"] = $"traces | where user_Id  == '{userId}' | project " +
                $"timestamp, message, customDimensions, cloud_RoleName, severityLevel";
            var response = await _httpClient.GetAsync($"v1/apps/{applicationId}/query?{query}");
            var responseObj = JsonConvert.DeserializeObject<UserLogQueryResponse>(await response.Content.ReadAsStringAsync());
            return responseObj.tables[0].rows.Select(row =>
            {
                var additionalData = null != row[2] ? JsonConvert.DeserializeObject<Dictionary<string, string>>((string)row[2]) : null;
                string digitTraceActionString = null;
                string focusItemId = null;
                string id = null;
                var severityLevel = (SeverityLevel)((long)row[4]);
                if (null != additionalData)
                {
                    additionalData.TryGetValue("digitTraceAction", out digitTraceActionString);
                    additionalData.TryGetValue("focusItemId", out focusItemId);
                    additionalData.TryGetValue("digitTraceId", out id);
                }
                var digitTraceAction = null != digitTraceActionString ?
                    (DigitTraceAction)Enum.Parse(typeof(DigitTraceAction), digitTraceActionString) : DigitTraceAction.Default;
                return new LogEntry()
                {
                    Timestamp = new DateTimeOffset((DateTime)row[0]),
                    Message = (string)row[1],
                    AdditionalData = additionalData,
                    Author = (string)row[3],
                    DigitTraceAction = digitTraceAction,
                    FocusItemId = focusItemId,
                    Id = id,
                    LogLevel = FromSeverity(severityLevel),
                    UserId = userId
                };
            }).ToArray();
        }

        private LogLevel FromSeverity(SeverityLevel s)
        {
            switch (s)
            {
                case SeverityLevel.Error:
                    return LogLevel.Error;
            }
            return LogLevel.Information;
        }
    }
}