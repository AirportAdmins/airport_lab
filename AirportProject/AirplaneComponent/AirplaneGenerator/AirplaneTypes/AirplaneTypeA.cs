using System;
using System.Collections.Generic;
using System.Text;
using AirplaneComponent.AirplaneGenerator;

namespace AirplaneComponent.AirplaneGenerator.AirplaneTypes
{
    public class AirplaneTypeA : Airplane, IAirplane
    {
        public AirplaneTypeA(int id)
        {
            this.airplaneID = "Plane-" + id;
        }
        public override string Name => "A";

        public override int PassengerCapacity => 50;

        public override int BaggageCapacity => 200;

        public override int MaxFuelAmount => 210;

    }
}
