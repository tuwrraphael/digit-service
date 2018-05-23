using DigitService.Models;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface ILocationService
    {
        Task<Location> GetCurrentLocation(string userId);
        Task LocationCallback(string userId, Location location);
    }
}
