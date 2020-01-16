using System;
using System.Collections.Generic;
using System.Text;
using AirportLibrary;
using System.Linq;
using RabbitMqWrapper;
using System.Collections.Concurrent;
using AirportLibrary.DTO;
using NLog;


namespace LogsComponent
{
    public class LogsComponent
    {
        //private readonly ILogger<>

        RabbitMqClient MqClient;
        Dictionary<string, string> queuesTo;
        Dictionary<string, string> queuesFrom;
        
        public void Start()
        {
            MqClient = new RabbitMqClient();
            CreateQueues();
            DeclareQueues();
            //MqClient.PurgeQueues(queuesFrom);
            Subscribe();
        }
        void CreateQueues()
        {
            queuesFrom = new Dictionary<string, string>()
            {
                { Component.GroundService, Component.GroundService + Component.Logs},
                { Component.FollowMe, Component.FollowMe + Component.Logs},
                { Component.Schedule, Component.Schedule + Component.Logs },
                { Component.FuelTruck, Component.FuelTruck + Component.Logs },
                { Component.Catering, Component.Catering +Component.Logs },
                { Component.Deicing, Component.Deicing + Component.Logs },
                { Component.GroundMotion,Component.GroundMotion+ Component.Logs },
                { Component.Bus, Component.Bus + Component.Logs },
                { Component.Baggage, Component.Baggage + Component.Logs },
                { Component.Airplane, Component.Baggage + Component.Logs },
                { Component.Cashbox, Component.Baggage + Component.Logs },
                { Component.Registration, Component.Baggage + Component.Logs },
                { Component.Storage, Component.Baggage + Component.Logs },
                { Component.TimeService, Component.Baggage + Component.Logs },
                { Component.Passenger, Component.Baggage + Component.Logs },
                { Component.Timetable, Component.Baggage + Component.Logs },
            };
        }
        void Subscribe()
        {
           foreach(var queue in queuesFrom.Values)
           {
                MqClient.SubscribeTo<LogMessage>(queue, mes => Log(mes));
           }
        }
        void Log(LogMessage mes)
        {

        }
        void DeclareQueues()
        {
            MqClient.DeclareQueues(queuesFrom.Values.ToArray());
        }
    }
}
