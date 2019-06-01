using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;

namespace DigitService
{
    public class RequestFilterTelemetryProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;

        public RequestFilterTelemetryProcessor(ITelemetryProcessor next)
        {
            _next = next;
        }

        public void Process(ITelemetry item)
        {
            RequestTelemetry requestTelemetry = item as RequestTelemetry;
            if (null != requestTelemetry)
            {
                if (!requestTelemetry.Success.HasValue || !requestTelemetry.Success.Value)
                {
                    _next.Process(item);
                }
            }
            else
            {
                _next.Process(item);
            }
        }
    }
}
