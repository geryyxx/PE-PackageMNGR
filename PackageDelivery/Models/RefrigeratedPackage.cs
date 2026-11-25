using System;
using System.Collections.Generic;
using System.Text;

namespace PackageDelivery.Models
{
    public class RefrigeratedPackage : Package
    {
        public double MinTemperatureCelsius { get; set; }

        public double MaxTemperatureCelsius { get; set; }
    }
}
