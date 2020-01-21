using System;
using System.Collections.Generic;
using System.Text;
using TransportMotion;
using AirportLibrary.DTO;
using System.Threading;

namespace CateringComponent
{
    public enum Status { Free, Busy}
    public class CateringCar : ICar
    {
        string carId;
        static List<Tuple<Food, int>> maxFoodAmount= new List<Tuple<Food, int>>()
            {
                Tuple.Create(Food.Child, 15),
                Tuple.Create(Food.Standard, 30),
                Tuple.Create(Food.Vegan, 15)
            };

        public CateringCar(int id)
        {
            carId = "Catering-" + id;
            //Status = Status.Free;           
        }
        public string CarId => carId ;                
        //public Status Status { get; set; }
        public string PlaneId { get; set; }
        public int Speed { get => 40; set { } }
        public int LocationVertex { get; set; }
        //public List<Tuple<Food, int>> FoodList { get; set; }
        public static List<Tuple<Food, int>> MaxFoodAmount { get => maxFoodAmount; }
        public AutoResetEvent MotionPermission { get; set; }
    }
}
