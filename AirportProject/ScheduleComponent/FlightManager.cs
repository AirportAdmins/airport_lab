using System;
using System.Collections.Generic;
using System.Text;
using AirportLibrary.DTO;

namespace ScheduleComponent
{
    class FlightManager : IFlightManager
    {
        const string FLIGHT_PREFIX = "FL";
        public const int MAX_PERIOD_BETWEEN_FLIGHTS_MS = 60 * 60 * 1000;
        public const int MIN_PERIOD_BETWEEN_FLIGHTS_MS = 20 * 60 * 1000;
        public const int MIN_TIME_BEFORE_DEPARTURE_MS = 15 * 60 * 1000;
        const int CHECK_IN_STARTS_BEFORE_MS = 60 * 60 * 1000;
        const int BOARDING_STARTS_BEFORE_MS = 30 * 60 * 1000;
        DateTime nextFlightGenerationTime = DateTime.MinValue;
        List<IFlight> flights = new List<IFlight>();
        Queue<IFlight> updatedFlights = new Queue<IFlight>();
        Queue<IFlight> flightsToDeparture = new Queue<IFlight>();
        IFlightGenerator flightGenerator = new FlightGenerator(FLIGHT_PREFIX);
        public IEnumerable<IFlight> GetFlightChanges()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IFlight> GetFlightsToDeparture()
        {
            throw new NotImplementedException();
        }

        public void SetCurrentTime(DateTime currentTime)
        {
            foreach (var flight in flights)
            {
                if (flight.Status == FlightStatus.New && 
                    currentTime.AddMilliseconds(CHECK_IN_STARTS_BEFORE_MS) >= flight.DepartureTime)
                {
                    updatedFlights.Enqueue(flight);
                } else if (flight.Status == FlightStatus.CheckIn &&
                    currentTime.AddMilliseconds(BOARDING_STARTS_BEFORE_MS) >= flight.DepartureTime)
                {
                    updatedFlights.Enqueue(flight);
                } else if (flight.Status == FlightStatus.Boarding &&
                    currentTime >= flight.DepartureTime)
                {
                    flightsToDeparture.Enqueue(flight);
                }
            }
            if (nextFlightGenerationTime < currentTime)
            {
                var random = new Random();
                nextFlightGenerationTime = currentTime.AddMilliseconds(
                    random.Next(MIN_PERIOD_BETWEEN_FLIGHTS_MS, MAX_PERIOD_BETWEEN_FLIGHTS_MS)
                );

                var temp = currentTime.AddMilliseconds(MIN_TIME_BEFORE_DEPARTURE_MS);
                DateTime departureTime = temp.Date;
                if (temp.Minute <= 30)
                {
                    departureTime += new TimeSpan(temp.Hour, 30, 0);
                }
                else
                {
                    temp = temp.AddHours(1);
                    departureTime = temp.Date + new TimeSpan(temp.Hour, 0, 0);
                }

                var model = AirplaneModel.Models[random.Next(AirplaneModel.Models.Count)];

                var flight = flightGenerator.GenerateFlight(
                     departureTime,
                     model
                );
                flights.Add(flight);
                updatedFlights.Enqueue(flight);
            }
        }

        public void SetPlaneForFlight(string flightId, string planeId)
        {
            flights.Find(flight => flight.FlightId == flightId).PlaneId = planeId;
        }

        public void UpdateStatusByPlaneId(string planeId, ServiceStatus status)
        {
            var toUpdate = flights.Find(flight => flight.PlaneId == planeId);
            switch (status)
            {
                case ServiceStatus.Delayed:
                    toUpdate.Status = FlightStatus.Delayed;
                    break;
                case ServiceStatus.Departed:
                    toUpdate.Status = FlightStatus.Departed;
                    break;
                default:
                    Console.WriteLine("Unknown ServiceStatus of {0} argument: {1}", nameof(status), status);
                    throw new ArgumentException($"Unknown ServiceStatus of {nameof(status)} argument: {status}");
            }
            updatedFlights.Enqueue(toUpdate);
        }
    }

    class FlightGenerator : IFlightGenerator
    {
        int nextFlightId;
        public FlightGenerator(string flightPrefix) : base(flightPrefix) {}
        public override IFlight GenerateFlight(DateTime departureTime, AirplaneModel model)
        {
            return new Flight()
            {
                FlightId = String.Format("{0}-{1}", FlightPrefix, nextFlightId++),
                Model = model,
                DepartureTime = departureTime,
                Status = FlightStatus.New
            };
        }
    }

    class Flight : IFlight
    {
        public string FlightId { get; set; }
        public string PlaneId { get; set; }
        public FlightStatus Status { get; set; }
        public DateTime DepartureTime { get; set; }
        public AirplaneModel Model { get; set; }
    }
}
