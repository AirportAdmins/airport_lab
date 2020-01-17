using System;
using System.Collections.Generic;
using System.Text;

namespace FuelTruck
{
    public enum Status
    {
        Free, Busy
    }
    class FuelTruckCars
    {
        public string CarID { get; set;  }
        public Status Status { get; set; }
        public int Position { get; set; }
        public int FuelOnBoard;
        public const int MaxFuelOnBoard = 1000;

        

    }
}
