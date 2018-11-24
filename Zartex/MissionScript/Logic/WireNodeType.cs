using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zartex
{
    public enum WireNodeType
    {
        OnSuccessEnable = 1,
        OnSuccessDisable = 2,

        OnFailureEnable = 3,
        OnFailureDisable = 4,

        OnConditionEnable = 5,
        OnConditionDisable = 6,

        GroupEnable = 11,
        GroupDisable = 12
    }
}
