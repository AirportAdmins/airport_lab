using System;
using System.Collections.Generic;
using System.Text;

namespace AirplaneComponent
{
    interface IAirplane
    {
        string Name { get; }
        int PassengerCapacity { get; }
        int BaggageCapacity { get; }
        int MaxFuelAmount { get; }      
    }
}
