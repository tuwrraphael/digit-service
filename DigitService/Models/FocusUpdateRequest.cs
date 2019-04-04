using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Models
{
    public class FocusUpdateRequest
    {
        public Location Location { get; set; }
        public FocusItemSyncResult ItemSyncResult { get; set; }
    }
}
