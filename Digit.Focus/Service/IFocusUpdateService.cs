using Digit.Focus.Model;
using System.Threading.Tasks;

namespace Digit.Focus.Service
{
    public interface IFocusUpdateService
    {
        Task<FocusManageResult> Update(string userId, FocusUpdateRequest focusUpdateRequest);
    }
}
