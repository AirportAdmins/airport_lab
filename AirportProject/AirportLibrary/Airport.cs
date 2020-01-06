using System;

namespace AirportLibrary
{
    public enum Component
    {
        Schedule,
        Airplane,
        GroundService,
        Timetable,
        Cashbox,
        Registration,
        Storage,
        Passenger,
        GroundMotion,
        Bus,
        Baggage,
        FollowMe,
        Catering,
        Deicing,
        FuelTruck,
        TimeService,
        Visualizer,
        Logs,
    }
    public static class Airport
    {
        
    }

    // Class for generating queue names
    public class Queue
    {
        Component from;
        private Queue(Component from)
        {
            this.from = from;
        }
        public static Queue From(Component from)
        {
            return new Queue(from);
        }
        public string To(Component to)
        {
            return from.ToString() + "-" + to.ToString();
        }
        public static string FromAnyTo(Component to)
        {
            return to.ToString();
        }

    }
}
