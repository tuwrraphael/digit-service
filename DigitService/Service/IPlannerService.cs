using System;
using System.Threading.Tasks;
using DigitService.Models;

namespace DigitService.Service
{
    public interface IPlannerService
    {
        Task<Plan> GetPlan(string userId, DateTimeOffset from, DateTimeOffset to);
    }
}