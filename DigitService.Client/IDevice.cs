using System.Threading.Tasks;

namespace DigitService.Client
{
    public interface IDevice
    {
        Task<bool> Claim();

        IBattery Battery { get; }
    }
}
