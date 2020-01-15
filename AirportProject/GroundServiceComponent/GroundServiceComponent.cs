using System;
using AirportLibrary;
using AirportLibrary.DTO;
using System.Collections.Generic;
using RabbitMqWrapper;

namespace GroundServiceComponent
{
    public class GroundServiceComponent
    {
        public static readonly string ComponentName = Component.GroundService;
        public static readonly List<string> Receivers = new List<string>()
        {
            Component.Airplane,
            Component.FollowMe,
            Component.Bus,
            Component.Baggage,
            Component.FuelTruck,
            Component.Catering,
            Component.Deicing,
            Component.Schedule
        };
        public static readonly List<string> Senders = new List<string>()
        {
            Component.Airplane,
            Component.FollowMe,
            Component.Bus,
            Component.Baggage,
            Component.FuelTruck,
            Component.Catering,
            Component.Deicing,
            Component.Schedule,
            Component.Registration
        };
        public static readonly List<string> GroundTransport = new List<string>()
        {
            Component.FollowMe,
            Component.Bus,
            Component.Baggage,
            Component.FuelTruck,
            Component.Catering,
            Component.Deicing
        };

        RabbitMqClient mqClient = new RabbitMqClient();
        
        public GroundServiceComponent()
        {
        }
        public void FisrtCycle(AirplaneServiceCommand mes)
        {

        }
        public void SecondCycle(FlightInfo mes)
        {

        }
        public void Start()
        {
            //Declare queues
            foreach (var receiver in Receivers)
                mqClient.DeclareQueues(ComponentName+receiver);
            foreach (var sender in Senders)
                mqClient.DeclareQueues(sender+ComponentName);

            //Receieve from airplane that fisrt cycle begin
            mqClient.SubscribeTo<AirplaneServiceCommand>(Component.Airplane + ComponentName, (mes) =>
            {
                FisrtCycle(mes);
            }
            );

            //Receieve from registration that second cycle begin 
            mqClient.SubscribeTo<FlightInfo>(Component.Registration + ComponentName, (mes) =>
            {
                SecondCycle(mes);
            }
            );

            //Receieve from Schedule that time to Departure
            mqClient.SubscribeTo<AirplaneServiceSignal>(Component.Schedule + ComponentName, (mes) =>
            {
                //
            }
            );

            //Subscribe to receive from GroundTransport
            foreach (var car in GroundTransport)
                mqClient.SubscribeTo<ServiceCompletionMessage>(car + ComponentName, (mes) =>
                  {

                  }
                );
        }
    }
}
