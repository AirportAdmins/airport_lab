using System;
using System.Collections.Generic;
using System.Text;
using AirplaneComponent.AirplaneGenerator;
using AirplaneComponent.AirplaneGenerator.AirplaneTypes;


namespace AirplaneComponent.Airplanes
{
    public class AirplaneGenerator
    {
        static int aiplaneID = 0;
        int flightID;
        Random rand;
        public Airplane Generate(string type,int flightID)
        {
            rand = new Random();
            this.flightID = flightID;
            Airplane airplane = null;
            switch (type)
            {
                case "A":
                    airplane=MakeAirplaneTypeA();
                    break;
                case "B":
                    airplane=MakeAirplaneTypeB();
                    break;
                case "C":
                    airplane=MakeAirplaneTypeC();
                    break;
            }
            return airplane;
        }
        
        Airplane FillAirplane(Airplane airplane)
        {
            airplane.NumberOfPassengers = rand.Next(0, airplane.PassengerCapacity);
            airplane.BaggageAmount = rand.Next(0, airplane.BaggageCapacity);
            airplane.FuelAmout = rand.Next(1, airplane.MaxFuelAmount);
            airplane.FlightID = flightID;
            return airplane;
        }
        Airplane MakeAirplaneTypeA()
        {
            Airplane airplane = new AirplaneTypeA(aiplaneID);
            aiplaneID++;
            return FillAirplane(airplane);
        }
        Airplane MakeAirplaneTypeB()
        {
            Airplane airplane = new AirplaneTypeB(aiplaneID);
            aiplaneID++;
            return FillAirplane(airplane);
        }
        Airplane MakeAirplaneTypeC()
        {
            Airplane airplane = new AirplaneTypeC(aiplaneID);
            aiplaneID++;
            return FillAirplane(airplane);
        }
    }
}
