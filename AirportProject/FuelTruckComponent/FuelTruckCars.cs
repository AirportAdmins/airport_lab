using System;
using System.Collections.Generic;
using System.Text;
using TransportMotion;

namespace FuelTruck
{
    public enum Status { Free, Busy }
    public class FuelTruckCar : ICar
    {
        string carId;
        

        public FuelTruckCar(int id) // CateringCar = FuelTrackCar
        {
            carId = "FuelTruck-" + id;
            //Status = Status.Free;           
        }
        public string CarId => carId;
        //public Status Status { get; set; }
        public string PlaneId { get; set; }
        public bool IsGoingHome { get; set; }
        public int Speed { get => 40; set { } }
        public int LocationVertex { get; set; }
        //public List<Tuple<Food, int>> FoodList { get; set; }
        //public static List<Tuple<Food, int>> MaxFoodAmount { get => maxFoodAmount; }
        public int FuelOnBoard { get; set; }

        public static int MaxFuelOnBoard = 500;
        public bool MotionPermitted { get; set; }
    }
}
