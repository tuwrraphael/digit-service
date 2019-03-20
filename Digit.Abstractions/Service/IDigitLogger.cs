using Digit.Abstractions.Models;
using System.Threading.Tasks;

namespace Digit.Abstractions.Service
{
    public interface IDigitLogger
    {
        Task Log(string user, string message, int code = 0);
        Task Log(string user, LogEntry entry);
    }
}

