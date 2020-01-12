using System;
using System.Collections.Generic;
using System.Text;
using AirportLibrary.DTO;

namespace PassengerComponent.Passengers
{
    class Passenger
    {
        public string PassengerId { get; set; }
        public bool HasTicket { get; set; }
        public bool HasBaggage { get; set; }
    }
}
