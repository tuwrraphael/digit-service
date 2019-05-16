using Digit.Focus.Models;
using System.Threading.Tasks;

namespace Digit.Focus.Service
{
    public interface IFocusExternalDataService
    {
        Task<FocusItemWithExternalData> Get(string userId, string id);
    }
}
