using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface IDigitAuthTokenService
    {
        Task<string> GetTokenAsync();
    }
}
