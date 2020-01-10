using AirportLibrary.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScheduleComponent
{
    interface IFlightManager
    {
        IEnumerable<IFlight> GetFlightChanges();
        IEnumerable<IFlight> GetFlightsToDeparture();
        void SetCurrentTime(DateTime currentTime);
        void UpdateStatusByPlaneId(string planeId, ServiceStatus status);
        void SetPlaneForFlight(string flightId, string planeId);
    }

    abstract class IFlightGenerator
    {
        protected string FlightPrefix { get; }
        public IFlightGenerator(string flightPrefix)
        {
            FlightPrefix = flightPrefix;
        }
        public abstract IFlight GenerateFlight(DateTime departureTime, AirplaneModel model);
    }

    interface IFlight
    {
        string FlightId { get; set; }
        string PlaneId { get; set; }
        FlightStatus Status { get; set; }
        DateTime DepartureTime { get; set; }
        AirplaneModel Model { get; set; }
    }
}
