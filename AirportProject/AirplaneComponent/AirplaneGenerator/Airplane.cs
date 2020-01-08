using System;
using System.Collections.Generic;
using System.Text;
using AirportLibrary.DTO;

namespace AirplaneComponent.AirplaneGenerator
{
    public class MotionData
    {
        public double Speed { get; set; }
        public int LocationVertex { get; set; }
    }
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
        MotionData GroundData { get; set; }

        //properties to be filled
        public int Passengers { get; set; }
        public int BaggageAmount { get; set; }
        public int FuelAmout { get; set; }
        public bool IsDeiced { get; set; }
        protected int FoodAmount { get; set; }

    }
}
