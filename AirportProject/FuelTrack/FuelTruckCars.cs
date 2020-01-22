using System;
using System.Collections.Generic;
using System.Text;

namespace FuelTruck
{
    public enum Status { Free, Busy }
    public class FuelTruckCar : ICar
    {
        string carId;
        

        public FuelTruckCar(int id) // CateringCar = FuelTrackCar
        {
            carId = "FuelTrack-" + id;
            //Status = Status.Free;           
        }
        public string CarId => carId;
        //public Status Status { get; set; }
        public string PlaneId { get; set; }
        public int Speed { get => 40; set { } }
        public int LocationVertex { get; set; }
        //public List<Tuple<Food, int>> FoodList { get; set; }
        //public static List<Tuple<Food, int>> MaxFoodAmount { get => maxFoodAmount; }
        public static int FuelOnBoard { get; set; }
        //const int MaxFuelOnBoard = 1000;
        public bool MotionPermission { get; set; }
    }
}
