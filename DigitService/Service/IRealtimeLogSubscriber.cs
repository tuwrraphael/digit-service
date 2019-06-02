using Digit.Abstractions.Models;
using System.Threading.Tasks;

namespace DigitService.Service
{

    public interface IRealtimeLogSubscriber
    {
        Task Add(LogEntry entry);
    }
}
