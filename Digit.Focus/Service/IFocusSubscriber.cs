using Digit.Focus.Model;
using Digit.Focus.Models;
using System.Threading.Tasks;

namespace Digit.Focus.Service
{
    public interface IFocusSubscriber
    {
        Task ActiveItemChanged(string userId, FocusItemWithExternalData currentItem);
        Task ActiveItemsChanged(string userId, FocusManageResult manageResult);
    }
}
