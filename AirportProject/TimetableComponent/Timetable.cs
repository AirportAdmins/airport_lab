using AirportLibrary.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimetableComponent
{
    interface ITimetable
    {
        void SetCurrentTime(DateTime currentTime);
        void UpdateFlight(FlightStatusUpdate flight);
        void Draw();
        List<FlightStatusUpdate> GetTimetable();
    }
    class ConsoleTimetable : ITimetable
    {
        readonly List<FlightStatusUpdate> flights = new List<FlightStatusUpdate>();
        DateTime currentTime;

        public void Draw()
        {
            Console.Clear();
            Console.WriteLine(currentTime);
            Console.WriteLine("============================================");
            Console.WriteLine("|| Flight || Status   || Departure time   ||");
            Console.WriteLine("============================================");
            foreach (var flight in flights)
            {
                Console.WriteLine("|| {0, -6} || {1, -8} || {2, -5} {3, -10} ||",
                    flight.FlightId,
                    flight.Status,
                    flight.Status == FlightStatus.Delayed ? "" : flight.DepartureTime.ToString("HH:mm"),
                    flight.Status == FlightStatus.Delayed ? "" : flight.DepartureTime.ToString("dd.MM.yyyy")
                );
            }
            if (flights.Count > 0)
                Console.WriteLine("============================================");
        }

        public void SetCurrentTime(DateTime currentTime)
        {
            this.currentTime = currentTime;
        }

        public void UpdateFlight(FlightStatusUpdate flight)
        {
            for (int i = 0; i < flights.Count; i++)
            {
                if (flights[i].FlightId == flight.FlightId)
                {
                    flights[i].Status = flight.Status;
                    return;
                }
            }
            flights.Add(flight);
        }
        public List<FlightStatusUpdate> GetTimetable()
        {
            return new List<FlightStatusUpdate>(flights);
        }

        public void RemoveFlight(string flightId)
        {
            flights.Remove(flights.Single(f => f.FlightId == flightId));
        }
    }
}
