using System.Threading.Tasks;

namespace DigitService.Client
{
    public interface IDevice
    {
        Task<bool> ClaimAsync();

        IBattery Battery { get; }
    }
}
