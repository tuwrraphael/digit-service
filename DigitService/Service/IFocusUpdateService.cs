using System.Threading.Tasks;
using DigitService.Models;

namespace DigitService.Service
{
    public interface IFocusUpdateService
    {
        Task<FocusManageResult> Update(string userId, FocusUpdateRequest focusUpdateRequest);
    }
}