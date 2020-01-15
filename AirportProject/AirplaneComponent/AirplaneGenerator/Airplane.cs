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
            MotionPermitted = false;
        }
        public AirplaneModel Model { get => model; }
        public string PlaneID { get => airplaneID; }
        public string FlightID { get; set; }

        //for motion
        public static int SpeedOnGround { get => 40; }         // km/hour
        public static int SpeedFly { get => 230; }
        public int LocationVertex { get; set; }
        public bool MotionPermitted { get; set; }

        //properties to be filled
        public int Passengers { get; set; }
        public int BaggageAmount { get; set; }
        public int FuelAmount { get; set; }
        public bool IsDeiced { get; set; }
        public List<Tuple<Food,int>> FoodList { get; set; }

    }
}
