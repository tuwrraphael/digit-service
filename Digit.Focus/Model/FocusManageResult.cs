using Digit.Focus.Models;
using System;
using System.Collections.Generic;

namespace Digit.Focus.Model
{
    public class FocusManageResult
    {
        public List<FocusItemWithExternalData> ActiveItems { get; set; } = new List<FocusItemWithExternalData>();
    }
}
