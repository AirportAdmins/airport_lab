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
            Component.Timetable
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
            Component.Timetable,
            Component.Registration
        };

        RabbitMqClient mqClient = new RabbitMqClient();
        
        public GroundServiceComponent()
        {
        }
        public void FisrtCycle()
        {

        }
        public void SecondCycle()
        {

        }
        public void Start()
        {
            foreach (var sender in Senders)
                mqClient.DeclareQueues(sender+ComponentName);

            mqClient.SubscribeTo<AirplaneServiceSignal>(Component.Airplane + ComponentName, (mes) =>
            {
                FisrtCycle();
            }
            );
            mqClient.SubscribeTo<AirplaneServiceSignal>(Component.Registration + ComponentName, (mes) =>
            {
                SecondCycle();
            }
            );
            mqClient.SubscribeTo<AirplaneServiceSignal>(Component.Timetable + ComponentName, (mes) =>
            {
                //Some
            }
            );
        }
    }
}
