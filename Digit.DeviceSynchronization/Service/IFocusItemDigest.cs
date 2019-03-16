using Digit.Focus.Models;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Service
{
    internal interface IFocusItemDigest
    {
        Task<string> GetDigestAsync(FocusItem item);
    }
}
