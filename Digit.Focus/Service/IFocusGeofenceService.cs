using System;
using System.Threading.Tasks;
using Digit.Focus.Model;

namespace Digit.Focus.Service
{
    public interface IFocusGeofenceService
    {
        Task RefreshGeofencesForActiveNavigations(string userId, FocusManageResult manageResult, DateTimeOffset now);
        Task UpdateFocusItems(string userId, Location newLocation);
    }
}