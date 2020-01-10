using System;
using System.Collections.Generic;
using System.Text;
using AirportLibrary.DTO;

namespace AirplaneComponent.AirplaneGenerator
{
    public class Airplane
    {
        AirplaneModel model;
        string airplaneID;

        public Airplane(AirplaneModel model,int id)
        {
            this.model = model;
            this.airplaneID = "Plane-" + id;
        }
        public AirplaneModel Model { get => model; }
        public string PlaneID { get => airplaneID; }
        public string FlightID { get; set; }

        //for motion
        public static int Speed { get; }         //TODO returns const
        public int LocationVertex { get; set; }

        //properties to be filled
        public int Passengers { get; set; }
        public int BaggageAmount { get; set; }
        public int FuelAmount { get; set; }
        public bool IsDeiced { get; set; }
        public List<Tuple<Food,int>> FoodList { get; set; }

    }
}
