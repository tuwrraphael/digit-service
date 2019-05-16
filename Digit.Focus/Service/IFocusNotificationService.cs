using Digit.Focus.Model;
using System.Threading.Tasks;

namespace Digit.Focus.Service
{
    public interface IFocusNotificationService
    {
        Task Notify(NotifyUserRequest request);
    }
}
