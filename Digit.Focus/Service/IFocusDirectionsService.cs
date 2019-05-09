using Digit.Focus.Models;
using System.Threading.Tasks;

namespace Digit.Focus.Service
{
    public interface IFocusDirectionsService
    {
        Task SetPlace(string userId, string itemId, SetPlaceRequest setPlaceRequest);
    }
}
