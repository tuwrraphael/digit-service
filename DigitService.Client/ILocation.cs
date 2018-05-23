using DigitService.Models;
using System.Threading.Tasks;

namespace DigitService.Client
{
    public interface ILocation
    {
        Task AddLocation(Location location);
        ILocation this[string userId] { get; }
    }
}
