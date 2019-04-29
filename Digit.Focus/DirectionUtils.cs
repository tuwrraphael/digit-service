using System.Linq;
using TravelService.Models.Directions;

namespace Digit.Focus
{
    public static class DirectionUtils
    {
        public static Route SelectRoute(DirectionsResult directionsResult)
        {
            return directionsResult?.TransitDirections?.Routes?.FirstOrDefault();
        }
    }
}
