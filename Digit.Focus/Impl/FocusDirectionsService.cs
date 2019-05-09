using Digit.Focus.Models;
using Digit.Focus.Service;
using System.Linq;
using System.Threading.Tasks;
using TravelService.Client;

namespace Digit.Focus.Impl
{
    public class FocusDirectionsService : IFocusDirectionsService
    {
        private readonly IFocusStore _focusStore;
        private readonly IFocusPatchService _focusService;
        private readonly ITravelServiceClient _travelServiceClient;

        public FocusDirectionsService(IFocusStore focusStore, IFocusPatchService focusService,
            ITravelServiceClient travelServiceClient)
        {
            _focusStore = focusStore;
            _focusService = focusService;
            _travelServiceClient = travelServiceClient;
        }


        public async Task SetPlace(string userId, string itemId, SetPlaceRequest setPlaceRequest)
        {
            if (null != setPlaceRequest.RememberForLocation)
            {
                await _travelServiceClient.Users[userId].Locations[setPlaceRequest.RememberForLocation].Put(new TravelService.Models.Coordinate()
                {
                    Lat = setPlaceRequest.Place.Lat,
                    Lng = setPlaceRequest.Place.Lng
                });
            }
            else if (null != setPlaceRequest.RememberForSubject)
            {
                await _travelServiceClient.Users[userId].Locations[$"#event:{setPlaceRequest.RememberForLocation}"].Put(new TravelService.Models.Coordinate()
                {
                    Lat = setPlaceRequest.Place.Lat,
                    Lng = setPlaceRequest.Place.Lng
                });
            }
            await _focusStore.SetPlaceForItem(userId, itemId, setPlaceRequest.Place);
            if ((await _focusStore.GetActiveAsync(userId)).Any(v => v.Id == itemId))
            {
                await _focusService.PatchAsync(userId);
            }
        }
    }
}
