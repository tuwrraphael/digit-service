using System.Threading.Tasks;

namespace Digit.Focus.Service
{
    public interface IFocusPatchService
    {
        Task PatchAsync(string userId);
    }
}
