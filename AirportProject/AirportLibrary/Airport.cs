using System;
using System.Collections.Generic;
using AirportLibrary.Graph;

namespace AirportLibrary
{
    public static class Component
    {
        public const string Schedule = "schedule";
        public const string Airplane = "airplane";
        public const string GroundService = "groundservice";
        public const string Timetable = "timetable";
        public const string Cashbox = "cashbox";
        public const string Registration = "registration";
        public const string Storage = "storage";
        public const string Passenger = "passenger";
        public const string GroundMotion = "groundmotion";
        public const string Bus = "bus";
        public const string Baggage = "baggage";
        public const string FollowMe = "followme";
        public const string Catering = "catering";
        public const string Deicing = "deicing";
        public const string FuelTruck = "fueltruck";
        public const string TimeService = "timeservice";
        public const string Visualizer = "visualizer";
        public const string Logs = "logs";
    }
    public static class Subject
    {
        public const string AirplaneTypes = "airplanetypes";
        public const string Baggage = "baggage";
        public const string Status = "status";
    }
    public class Map
    {
        public readonly Graph<int> Graph = new Graph<int>();
        public Map()
        {
            for (int i = 1; i <= 25; i++)
            {
                Graph.AddVertex(i);
            }

            Graph.AddEdge(1, 5, 200);
            Graph.AddEdge(2, 6, 200);
            Graph.AddEdge(3, 7, 200);
            Graph.AddEdge(4, 5, 100);
            Graph.AddEdge(4, 10, 50);
            Graph.AddEdge(5, 6, 100);
            Graph.AddEdge(5, 13, 100);
            Graph.AddEdge(6, 7, 100);
            Graph.AddEdge(6, 14, 100);
            Graph.AddEdge(7, 8, 100);
            Graph.AddEdge(7, 15, 100);
            Graph.AddEdge(8, 9, 100);
            Graph.AddEdge(8, 11, 50);
            Graph.AddEdge(9, 12, 50);
            Graph.AddEdge(10, 13, 150);
            Graph.AddEdge(10, 16, 50);
            Graph.AddEdge(13, 14, 100);
            Graph.AddEdge(13, 16, 150);
            Graph.AddEdge(13, 20, 100);
            Graph.AddEdge(14, 15, 100);
            Graph.AddEdge(14, 21, 100);
            Graph.AddEdge(15, 11, 150);
            Graph.AddEdge(11, 12, 100);
            Graph.AddEdge(11, 17, 50);
            Graph.AddEdge(12, 18, 50);
            Graph.AddEdge(16, 19, 50);
            Graph.AddEdge(15, 17, 150);
            Graph.AddEdge(15, 22, 100);
            Graph.AddEdge(17, 18, 100);
            Graph.AddEdge(18, 23, 50);
            Graph.AddEdge(18, 24, 50);
            Graph.AddEdge(19, 25, 200);
            Graph.AddEdge(20, 25, 150);
            Graph.AddEdge(21, 25, 100);
            Graph.AddEdge(22, 25, 150);
            Graph.AddEdge(23, 25, 200);
        }
        public List<int> FindShortcut(int v1, int v2)
        {
            return new Dijkstra<int>(Graph).FindShortcut(v1,v2);
        }
    }
}
