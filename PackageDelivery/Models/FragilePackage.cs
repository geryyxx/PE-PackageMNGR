using PackageDelivery.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PackageDelivery.Models
{
    public class FragilePackage : Package
    {
        public FragilityLevel Fragility { get; set; }

        public string HandlingInstructions { get; set; } = string.Empty;
    }
}
