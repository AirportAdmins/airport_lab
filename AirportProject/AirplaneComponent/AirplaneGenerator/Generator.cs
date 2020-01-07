using System;
using System.Collections.Generic;
using System.Text;
using AirplaneComponent.AirplaneGenerator;
using AirportLibrary.DTO;

namespace AirplaneComponent.Airplanes
{
    public class AirplaneGenerator
    {
        static int aiplaneID = 0;
        Random rand;
        public Airplane Generate(AirplaneModel model,int flightID)
        {
            rand = new Random();
            Airplane airplane = new Airplane(model,aiplaneID);
            aiplaneID++;
            airplane.FlightID = flightID;
            return FillAirplane(airplane);
        }
        
        Airplane FillAirplane(Airplane airplane)
        {
            airplane.NumberOfPassengers = rand.Next(0, airplane.Model.Seats);
            airplane.BaggageAmount = rand.Next(0, airplane.Model.BaggagePlaces);
            airplane.FuelAmout = rand.Next(1, airplane.Model.Fuel);
            return airplane;
        }
   
    }
}
