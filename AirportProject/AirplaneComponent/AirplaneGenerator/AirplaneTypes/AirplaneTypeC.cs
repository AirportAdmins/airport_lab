using System;
using System.Collections.Generic;
using System.Text;
using AirplaneComponent.AirplaneGenerator;

namespace AirplaneComponent.AirplaneGenerator.AirplaneTypes
{
    public class AirplaneTypeC : Airplane
    {
        public AirplaneTypeC(int id)
        {
            this.airplaneID = "Plane-" + id;
        }
        public override string Name => "C";

        public override int PassengerCapacity => 30;

        public override int BaggageCapacity => 50;

        public override int MaxFuelAmount => 60;
    }
}
