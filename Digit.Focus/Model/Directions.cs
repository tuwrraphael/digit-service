using TravelService.Models;

namespace Digit.Focus.Models
{
    public class DirectionsMetadata
    {
        public string Key { get; set; }
        public DirectionsNotFoundReason? Error { get; set; }
        public int PeferredRoute { get; set; }
    }


}
