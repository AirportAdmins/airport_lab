using System;
using System.Collections.Generic;
using System.Text;
using AirportLibrary.DTO;

namespace PassengerComponent.Passengers
{
    class Passenger
    {
        public string PassengerId { get; set; }
        public PassengerStatus Status { get; set; }
        public string FlightId { get; set; }
        public bool HasBaggage { get; set; }
        public Food FoodType { get; set; }
    }
}
