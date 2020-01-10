using AirportLibrary.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace TimetableComponent
{
    interface ITimetable
    {
        void SetCurrentTime(DateTime currentTime);
        void UpdateFlight(FlightStatusUpdate flight);
        void Draw();
    }
    class ConsoleTimetable : ITimetable
    {
        List<FlightStatusUpdate> flights;
        DateTime currentTime;

        public void Draw()
        {
            Console.Clear();
            Console.WriteLine(currentTime);
            Console.WriteLine("======================================");
            Console.WriteLine("||Flight || Status || Departure time||");
            Console.WriteLine("======================================");
            foreach (var flight in flights)
            {
                Console.WriteLine("||{0} {1} {2}||", 
                    flight.FlightId, 
                    flight.Status,  
                    flight.Status == FlightStatus.Delayed ? "" : flight.DepartureTime.ToString()
                );
            }
            Console.WriteLine("======================================");
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
    }
}
