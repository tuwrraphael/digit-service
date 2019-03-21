using Digit.Focus.Models;
using System.Threading.Tasks;

namespace Digit.Focus.Service
{
    public interface IFocusSubscriber
    {
        Task ActiveItemChanged(string userId, FocusItem currentItem);
        Task ActiveItemsChanged(string userId, FocusItem[] items);
    }
}
