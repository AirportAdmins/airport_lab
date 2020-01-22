using System;
using System.Collections.Generic;
using System.Text;

namespace FuelTruck
{
    public enum Status
    {
        Free, Busy
    }
    class FuelTruckCar
    {
        string fuelTruckId { get; set;  }
        public FuelTruckCar(int id)
        {
            fuelTruckID = "FuelTruck" + id;
            MotionPermitted = false;
            GotAirplaneResponse = false;
        }

        public string FuelTrackId { get => fuelTruckId; }
        public string PlaneId { get; set; }
        public Status Status { get; set; }
        public int FuelOnBoard { get; set; }

        const int MaxFuel = 1000;
        
        public bool MotionPermitted { get; set; }
        public bool GotAirplaneResponse { get; set; }
        public static int Speed { get => 50; }
        public int LocationVertex { get; set; }

    }
}
