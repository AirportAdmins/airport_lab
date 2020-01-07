﻿using RabbitMqWrapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace TimeServiceComponent
{
    class TimeServiceComponent
    {
        public static readonly List<string> CurrentTimeReceivers = new List<string>()
        {
            Component.TimeService + Component.Schedule,
            Component.TimeService + Component.Timetable,
            Component.TimeService + Component.Logs
        };
        public static readonly List<string> NewTimeSpeedFactorReceivers = new List<string>()
        {
            Component.TimeService + Component.Visualizer,
            Component.TimeService + Component.Bus,
            Component.TimeService + Component.Baggage,
            Component.TimeService + Component.Catering,
            Component.TimeService + Component.FollowMe,
            Component.TimeService + Component.FuelTruck,
            Component.TimeService + Component.Deicing,
            Component.TimeService + Component.Passenger,
            Component.TimeService + Component.Storage,
            Component.TimeService + Component.Cashbox,
            Component.TimeService + Component.Registration
        };
        public static double TimeSpeedFactor = 1.0;
        public const int CHANGE_FACTOR = 2;
        public const double MAX_SPEED_FACTOR = 4096;
        public const double MIN_SPEED_FACTOR = 0.015625;
        public void Start()
        {
            //var mqClient = new RabbitMqClient();

            //mqClient.DeclareQueues(CurrentTimeReceivers.ToArray());
            //mqClient.DeclareQueues(NewTimeSpeedFactorReceivers.ToArray());

            char ch;
            ShowInfoMessage();
            while (true)
            {
                ch = Console.ReadKey().KeyChar;
                Console.WriteLine();

                if (ch == 'q')
                    break;

                switch (ch)
                {
                    case 'u':
                    case 'U':
                        ChangeSpeed(Action.Increase);
                        break;
                    case 'd':
                    case 'D':
                        ChangeSpeed(Action.Decrease);
                        break;
                    default:
                        ShowInfoMessage();
                        break;
                }
            }
            Console.WriteLine();
        }
        public void ShowInfoMessage()
        {
            Console.WriteLine("Acceptable input:");
            Console.WriteLine("u/U to increase speed");
            Console.WriteLine("d/D to decrease speed");
            Console.WriteLine("q to quit");
        }
        public void ChangeSpeed(Action action)
        {
            string actionWord = null;
            bool allow = false;
            switch (action)
            {
                case Action.Increase:
                    actionWord = "increased";
                    allow = TimeSpeedFactor < MAX_SPEED_FACTOR;
                    if (allow)
                        TimeSpeedFactor *= CHANGE_FACTOR;
                    break;
                case Action.Decrease:
                    actionWord = "decreased";
                    allow = TimeSpeedFactor > MIN_SPEED_FACTOR;
                    if (allow)
                        TimeSpeedFactor /= CHANGE_FACTOR;
                    break;
            }
            if (allow)
            {
                Console.WriteLine(
                    "Time speed has been {0} by factor of {1}. It's now {2}",
                    actionWord,
                    CHANGE_FACTOR,
                    TimeSpeedFactor);
            }
            else
            {
                Console.WriteLine("Time speed has reached the limit: {0}", TimeSpeedFactor);
                Console.WriteLine("Max possible value: {0}", MAX_SPEED_FACTOR);
                Console.WriteLine("Min possible value: {0}", MIN_SPEED_FACTOR);
            }
        }
        public enum Action
        {
            Increase, Decrease
        }
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
    }
}
