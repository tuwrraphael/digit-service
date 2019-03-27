using Digit.Focus.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Digit.Focus.Service
{
    public static class IFocusStoreExtension
    {
        public static async Task<FocusItem> GetActiveItem(this IFocusStore focusStore, string userId)
        {
            FocusItem[] items = await focusStore.GetActiveAsync(userId);
            var query = items.Where(v => v.IndicateTime - DateTimeOffset.Now < FocusConstants.ItemActiveBeforeIndicateAlone);
            if (query.Count() == 1)
            {
                return query.Single();
            }
            return items
                .Where(v => v.IndicateTime - DateTimeOffset.Now < FocusConstants.ItemActiveBeforeIndicateMultiple)
                .OrderByDescending(v => v.IndicateTime)
                .FirstOrDefault();
        }
    }
}
