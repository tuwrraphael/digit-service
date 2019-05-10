using TravelService.Models;

namespace Digit.Focus.Models
{
    public class Directions
    {
        public string DirectionsKey { get; set; }
        public DirectionsNotFoundReason? DirectionsError { get; set; }
        public int? PeferredRoute { get; set; }
    }


}
