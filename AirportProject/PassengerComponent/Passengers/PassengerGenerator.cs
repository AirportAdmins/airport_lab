using AirportLibrary.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace PassengerComponent.Passengers
{
    class PassengerGenerator
    {
        int nextPassengerId;
        string PassengerPrefix { get; }
        public PassengerGenerator(string passengerPrefix)
        {
            PassengerPrefix = passengerPrefix;
        }
        public Passenger GeneratePassenger()
        {

            return new Passenger()
            {
                PassengerId = String.Format("{0}-{1}", PassengerPrefix, nextPassengerId++),
                Status = PassengerStatus.NoTicket,
            };
        }
    }
}
