using System.Linq;
using System.Security.Claims;

namespace DigitService
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetId(this ClaimsPrincipal p)
        {
            return p.Claims.Where(v => v.Type == "sub").Single().Value;
        }
    }
}
