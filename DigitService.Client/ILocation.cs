using DigitService.Models;
using System.Threading.Tasks;

namespace DigitService.Client
{
    public interface ILocation
    {
        Task AddLocationAsync(Location location);
        Task<Location> GetAsync();
        Task NotifyErrorAsync(LocationConfigurationError error);
        ILocation this[string userId] { get; }
    }
}
