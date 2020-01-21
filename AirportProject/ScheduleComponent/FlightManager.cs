using System;
using System.Collections.Generic;
using System.Text;
using AirportLibrary.DTO;

namespace ScheduleComponent
{
    class FlightManager : IFlightManager
    {
        const string FLIGHT_PREFIX = "FL";
        public const int MAX_PERIOD_BETWEEN_FLIGHTS_MS = 30 * 60 * 1000;
        public const int MIN_PERIOD_BETWEEN_FLIGHTS_MS = 15 * 60 * 1000;
        public const int MIN_TIME_BEFORE_DEPARTURE_MS = 45 * 60 * 1000;
        const int CHECK_IN_STARTS_BEFORE_MS = 60 * 60 * 1000;
        const int BOARDING_STARTS_BEFORE_MS = 30 * 60 * 1000;
        DateTime nextFlightGenerationTime = DateTime.MinValue;
        List<IFlight> flights = new List<IFlight>();
        Queue<IFlight> updatedFlights = new Queue<IFlight>();
        Queue<IFlight> flightsToSendDepartureRequest = new Queue<IFlight>();
        List<IFlight> flightsToDeparture = new List<IFlight>();
        IFlightGenerator flightGenerator = new FlightGenerator(FLIGHT_PREFIX);
        public IEnumerable<IFlight> GetFlightChanges()
        {
            lock (updatedFlights)
            {
                while (updatedFlights.Count > 0)
                    yield return updatedFlights.Dequeue();
            }
        }

        public IEnumerable<IFlight> GetFlightsToDeparture()
        {
            lock (flightsToSendDepartureRequest)
            {
                while (flightsToSendDepartureRequest.Count > 0)
                    yield return flightsToSendDepartureRequest.Dequeue();
            }
        }

        public void SetCurrentTime(DateTime currentTime)
        {
            lock (updatedFlights)
            {
                lock (flightsToDeparture)
                {
                    for (var i = flights.Count - 1; i >= 0; i--)
                    {
                        var flight = flights[i];
                        if (flight.Status == FlightStatus.New &&
                            currentTime.AddMilliseconds(CHECK_IN_STARTS_BEFORE_MS) >= flight.DepartureTime)
                        {
                            flight.Status = FlightStatus.CheckIn;
                            updatedFlights.Enqueue(new Flight()
                            {
                                FlightId = flight.FlightId,
                                DepartureTime = flight.DepartureTime,
                                PlaneId = flight.PlaneId,
                                Model = flight.Model,
                                Status = FlightStatus.CheckIn
                            });
                        }
                        if (flight.Status == FlightStatus.CheckIn &&
                            currentTime.AddMilliseconds(BOARDING_STARTS_BEFORE_MS) >= flight.DepartureTime)
                        {
                            flight.Status = FlightStatus.Boarding;
                            updatedFlights.Enqueue(new Flight()
                            {
                                FlightId = flight.FlightId,
                                DepartureTime = flight.DepartureTime,
                                PlaneId = flight.PlaneId,
                                Model = flight.Model,
                                Status = FlightStatus.Boarding
                            });
                        }
                        if (flight.Status == FlightStatus.Boarding &&
                            currentTime >= flight.DepartureTime)
                        {
                            flights.RemoveAt(i);
                            flightsToDeparture.Add(flight);
                            flightsToSendDepartureRequest.Enqueue(flight);
                        }
                    }
                }
            }
            if (nextFlightGenerationTime < currentTime)
            {
                lock (updatedFlights)
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
                    //Console.WriteLine("Current: {0} New departure: {1}", currentTime, departureTime);

                    var model = AirplaneModel.Models[random.Next(AirplaneModel.Models.Count)];

                    var flight = flightGenerator.GenerateFlight(
                         departureTime,
                         model
                    );
                    flights.Add(flight);
                    updatedFlights.Enqueue(new Flight()
                    {
                        FlightId = flight.FlightId,
                        DepartureTime = flight.DepartureTime,
                        PlaneId = flight.PlaneId,
                        Model = flight.Model,
                        Status = FlightStatus.New
                    });
                }
            }
        }

        public void SetPlaneForFlight(string flightId, string planeId)
        {
            flights.Find(flight => flight.FlightId == flightId).PlaneId = planeId;
        }

        public void UpdateStatusByPlaneId(string planeId, ServiceStatus status)
        {
            lock (updatedFlights)
            {
                lock (flightsToDeparture)
                {
                    var toUpdate = flightsToDeparture.Find(flight => flight.PlaneId == planeId);
                    switch (status)
                    {
                        case ServiceStatus.Delayed:
                            toUpdate.Status = FlightStatus.Delayed;
                            break;
                        case ServiceStatus.Departed:
                            toUpdate.Status = FlightStatus.Departed;
                            flightsToDeparture.Remove(toUpdate);
                            break;
                        default:
                            Console.WriteLine("Unknown ServiceStatus of {0} argument: {1}", nameof(status), status);
                            throw new ArgumentException($"Unknown ServiceStatus of {nameof(status)} argument: {status}");
                    }
                    updatedFlights.Enqueue(toUpdate);
                }
            }
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
