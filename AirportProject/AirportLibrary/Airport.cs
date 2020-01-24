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
        public const string Factor = "factor";
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

            Graph.AddEdge(1, 5, 286);
            Graph.AddEdge(2, 6, 286);
            Graph.AddEdge(3, 7, 286);
            Graph.AddEdge(4, 5, 156);
            Graph.AddEdge(5, 6, 156);
            Graph.AddEdge(5, 13, 173);
            Graph.AddEdge(6, 7, 156);
            Graph.AddEdge(6, 14, 173);
            Graph.AddEdge(7, 8, 156);
            Graph.AddEdge(7, 15, 173);
            Graph.AddEdge(8, 9, 156);
            Graph.AddEdge(8, 11, 86);
            Graph.AddEdge(9, 12, 86);
            Graph.AddEdge(10, 13, 178);
            Graph.AddEdge(13, 14, 156);
            Graph.AddEdge(13, 16, 178);
            Graph.AddEdge(13, 20, 173);
            Graph.AddEdge(14, 15, 156);
            Graph.AddEdge(14, 21, 173);
            Graph.AddEdge(15, 11, 178);
            Graph.AddEdge(11, 12, 156);
            Graph.AddEdge(11, 17, 173);
            Graph.AddEdge(12, 18, 173);
            Graph.AddEdge(15, 17, 178);
            Graph.AddEdge(15, 22, 173);
            Graph.AddEdge(17, 18, 156);
            Graph.AddEdge(18, 24, 86);
            Graph.AddEdge(19, 25, 356);
            Graph.AddEdge(20, 25, 233);
            Graph.AddEdge(21, 25, 173);
            Graph.AddEdge(22, 25, 233);
            Graph.AddEdge(23, 25, 357);
            Graph.AddEdge(24, 25, 499);
            Graph.AddEdge(23, 24, 156);
            Graph.AddEdge(22, 23, 156);
            Graph.AddEdge(21, 22, 156);
            Graph.AddEdge(20, 21, 156);
            Graph.AddEdge(19, 20, 156);
            Graph.AddEdge(17, 23, 86);
        }
        public List<int> FindShortcut(int v1, int v2)
        {
            return new Dijkstra<int>(Graph).FindShortcut(v1,v2);
        }
    }
}
