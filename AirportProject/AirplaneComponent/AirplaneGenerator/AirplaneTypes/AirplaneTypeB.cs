using System;
using System.Collections.Generic;
using System.Text;
using AirplaneComponent.AirplaneGenerator;

namespace AirplaneComponent.AirplaneGenerator.AirplaneTypes
{
    public class AirplaneTypeB : Airplane, IAirplane
    {
        public AirplaneTypeB(int id)
        {
            this.airplaneID = "Plane-" + id;
        }
        public override string Name => "B";

        public override int PassengerCapacity => 40;

        public override int BaggageCapacity => 100;

        public override int MaxFuelAmount => 110;
    }
}
