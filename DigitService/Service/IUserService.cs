using DigitService.Models;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface IUserService
    {
        Task<UserInformation> GetInformationAsync(string userId);
        Task<UserInformation> CreateAsync(string userId);
    }
}
