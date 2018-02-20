using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface IDigitLogger
    {
        Task Log(string user, string message, int code = 0);
    }
}
