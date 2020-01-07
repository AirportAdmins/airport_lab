using AirportLibrary.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScheduleComponent
{
    interface IFlightManager
    {
        void GenerateNewFlight();
        IEnumerable<IFlight> GetFlightChanges();
        void SetCurrentTime(DateTime currentTime);
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
