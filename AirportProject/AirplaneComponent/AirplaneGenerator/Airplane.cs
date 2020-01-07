using System;
using System.Collections.Generic;
using System.Text;
using AirportLibrary.DTO;

namespace AirplaneComponent.AirplaneGenerator
{
    public class Airplane
    {
        AirplaneModel model;

        int passengers;
        int baggageAmount;
        int fuelAmout;
        bool isDeiced;
        int foodAmount;
        string airplaneID;
        string flightID;

        public Airplane(AirplaneModel model,int id)
        {
            this.model = model;
            this.airplaneID = "Plane-" + id;
        }
        public string PlaneID { get => airplaneID; }
        public string FlightID { get => flightID; set => flightID = value; }
        public AirplaneModel Model { get => model; }

        //properties to be filled
        public int Passengers { get => passengers; set => passengers = value; }
        public int BaggageAmount { get => baggageAmount; set => baggageAmount = value; }
        public int FuelAmout { get => fuelAmout; set => fuelAmout = value; }
        public bool IsDeiced { get => isDeiced; set => isDeiced = value; }
        protected int FoodAmount { get => foodAmount; set => foodAmount = value; }
    }
}
