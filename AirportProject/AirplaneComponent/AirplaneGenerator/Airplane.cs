using System;
using System.Collections.Generic;
using System.Text;
using AirportLibrary.DTO;

namespace AirplaneComponent.AirplaneGenerator
{
    public class Airplane
    {
        AirplaneModel model;

        int numberOfPassengers;
        int baggageAmount;
        int fuelAmout;
        bool isDeiced;
        int foodAmount;
        string airplaneID;
        int flightID;

        public Airplane(AirplaneModel model)
        {
            this.model = model;
        }
        public string AirplaneID { get => airplaneID; }
        public int FlightID { get => flightID; set => flightID = value; }
        public AirplaneModel Model { get => model; }

        //properties to be filled
        public int NumberOfPassengers { get => numberOfPassengers; set => numberOfPassengers = value; }
        public int BaggageAmount { get => baggageAmount; set => baggageAmount = value; }
        public int FuelAmout { get => fuelAmout; set => fuelAmout = value; }
        public bool IsDeiced { get => isDeiced; set => isDeiced = value; }
        protected int FoodAmount { get => foodAmount; set => foodAmount = value; }
    }
}
