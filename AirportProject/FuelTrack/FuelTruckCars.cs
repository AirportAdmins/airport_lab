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
        public int CarID { get; }
        public Status Status { get; set; }
        public int Position { get; set; }
        public int FuelOnBoard;
        public const int MaxFuelOnBoard = 1000;

        public FuelTruckCars(int id, int pos)
        {
            CarID = id;
            Position = pos;
        }

    }
}
