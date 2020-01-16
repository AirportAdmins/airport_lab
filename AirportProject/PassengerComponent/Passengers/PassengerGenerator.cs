using AirportLibrary.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PassengerComponent.Passengers
{
    class PassengerGenerator
    {
        int nextPassengerId;
        Random random = new Random();
        string PassengerPrefix { get; }
        public PassengerGenerator(string passengerPrefix)
        {
            PassengerPrefix = passengerPrefix;
        }
        public Passenger GeneratePassenger()
        {
            var values = Enum.GetValues(typeof(Food)).Cast<Food>().ToList();
            return new Passenger()
            {
                PassengerId = String.Format("{0}-{1}", PassengerPrefix, nextPassengerId++),
                Status = PassengerStatus.NoTicket,
                HasBaggage = random.NextDouble() > 0.5,
                FoodType = values[random.Next(0, values.Count)]
            };
        }
    }
}
