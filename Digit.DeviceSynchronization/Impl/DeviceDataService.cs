using CalendarService.Client;
using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using Digit.Focus;
using Digit.Focus.Service;
using System;
using System.Linq;
using System.Threading.Tasks;
using TravelService.Client;
using TravelService.Models.Directions;

namespace Digit.DeviceSynchronization.Impl
{
    public class DeviceDataService : IDeviceDataService
    {
        private readonly IFocusStore _focusStore;
        private readonly IDeviceSyncStore _deviceSyncStore;
        private readonly ICalendarServiceClient _calendarServiceClient;
        private readonly ITravelServiceClient _travelServiceClient;

        public DeviceDataService(IFocusStore focusStore,
            IDeviceSyncStore deviceSyncStore,
            ICalendarServiceClient calendarServiceClient,
            ITravelServiceClient travelServiceClient)
        {
            _focusStore = focusStore;
            _deviceSyncStore = deviceSyncStore;
            _calendarServiceClient = calendarServiceClient;
            _travelServiceClient = travelServiceClient;
        }

        public async Task<DeviceData> GetDeviceData(string userId, string deviceId)
        {
            var owner = await _deviceSyncStore.DeviceClaimedByAsync(deviceId);
            if (owner != userId)
            {
                throw new DeviceAccessException(deviceId, userId, owner);
            }
            var activeItem = await _focusStore.GetActiveItem(userId);
            if (null == activeItem)
            {
                return new DeviceData()
                {
                    Event = null,
                    Directions = null
                };
            }
            if (null == activeItem.CalendarEventId && null == activeItem.CalendarEventFeedId)
            {
                return null; //other items are not supported yet
            }
            var evt = await _calendarServiceClient.Users[userId].Feeds[activeItem.CalendarEventFeedId].Events.Get(activeItem.CalendarEventId);
            if (null == evt)
            {
                return null;
            }
            Route route = null;
            if (null != activeItem.DirectionsKey)
            {
                route = DirectionUtils.SelectRoute(await _travelServiceClient.Directions[activeItem.DirectionsKey].GetAsync());
            }
            return new DeviceData()
            {
                Event = new EventData()
                {
                    Start = evt.Start,
                    Subject = evt.Subject
                },
                Directions = null != route ? new DirectionsData
                {
                    ArrivalTime = route.ArrivalTime,
                    DepartureTime = route.DepatureTime,
                    Legs = route.Steps.Select(v => new LegData()
                    {
                        ArrivalStop = v.ArrivalStop.Name,
                        DepartureStop = v.DepartureStop.Name,
                        DepartureTime = v.DepartureTime,
                        Direction = v.Headsign,
                        Line = v.Line.ShortName
                    }).ToArray()
                } : null
            };
        }

        public Task<DeviceSyncStatus> GetDeviceSyncStatus(string userId, string deviceId)
        {
            throw new NotImplementedException();
        }
    }
}
