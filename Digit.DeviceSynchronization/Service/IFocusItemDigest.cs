using Digit.Focus.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Digit.DeviceSynchronization.Service
{
    internal interface IFocusItemDigest
    {
        string GetDigest(FocusItem item);
    }
}
