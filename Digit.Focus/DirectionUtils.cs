using System.Linq;
using TravelService.Models.Directions;

namespace Digit.Focus
{
    public static class DirectionUtils
    {
        public static Route SelectRoute(DirectionsResult directionsResult)
        {
            if (null != directionsResult?.TransitDirections && directionsResult.TransitDirections.Routes.Any())
            {
                if (directionsResult.TransitDirections.Routes.Where(v => v.DepatureTime.HasValue).Any())
                {
                    var route = directionsResult.TransitDirections.Routes.Where(v => v.DepatureTime.HasValue).First();
                    return route;
                }
            }
            return null;
        }
    }
}
