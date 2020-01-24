using System;
using System.Collections.Generic;
using System.Text;
using AirplaneComponent.AirplaneGenerator;
using AirportLibrary.DTO;

namespace AirplaneComponent.AirplaneGenerator
{
    public class Generator
    {
        static int aiplaneID = 0;
        static Random rand;
        public static Airplane Generate(string modelName,string flightID)
        {
            rand = new Random();
            List<AirplaneModel> models = AirplaneModel.Models as List<AirplaneModel>;
            AirplaneModel modelNeeded = models.Find(m => m.Model == modelName);
            Airplane airplane = new Airplane(modelNeeded,aiplaneID);
            aiplaneID++;
            airplane.FlightID = flightID;
            return FillAirplane(airplane);
        }
        
        static Airplane FillAirplane(Airplane airplane)
        {
            airplane.Passengers = rand.Next(1, airplane.Model.Seats);
            airplane.BaggageAmount = rand.Next(1, airplane.Model.BaggagePlaces);
            airplane.FuelAmount = rand.Next(1, airplane.Model.Fuel);
            return airplane;
        }
   
    }
}
