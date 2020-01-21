using AirportLibrary;
using AirportLibrary.DTO;
using RabbitMqWrapper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            Component.TimeService + Component.Airplane,
            Component.TimeService + Component.Bus,
            Component.TimeService + Component.Baggage,
            Component.TimeService + Component.Catering,
            Component.TimeService + Component.FollowMe,
            Component.TimeService + Component.FuelTruck,
            Component.TimeService + Component.Deicing,
            Component.TimeService + Component.Passenger,
            Component.TimeService + Component.Storage,
            Component.TimeService + Component.Cashbox,
            Component.TimeService + Component.Registration,
            Component.TimeService + Component.Timetable + Subject.Factor
        };
        public static double TimeSpeedFactor = 1.0;
        public const int SEND_CURRENT_TIME_PERIOD_MS = 100;
        public const int CHANGE_FACTOR = 2;
        public const double MAX_SPEED_FACTOR = 1024;
        public const double MIN_SPEED_FACTOR = 0.015625;

        RabbitMqClient mqClient;

        DateTime playTime;
        public void Start()
        {
            mqClient = new RabbitMqClient();

            mqClient.DeclareQueues(CurrentTimeReceivers.ToArray());
            mqClient.DeclareQueues(NewTimeSpeedFactorReceivers.ToArray());

            var cancellationSource = new CancellationTokenSource();
            var token = cancellationSource.Token;

            Task.Run(() =>
            {
                playTime = DateTime.Now;
                while (!token.IsCancellationRequested)
                {
                    foreach (var queue in CurrentTimeReceivers)
                    {
                        mqClient.Send(queue, new CurrentPlayTime()
                        {
                            PlayTime = playTime
                        });
                    }
                    Thread.Sleep(SEND_CURRENT_TIME_PERIOD_MS);
                    playTime = playTime.AddMilliseconds(SEND_CURRENT_TIME_PERIOD_MS * TimeSpeedFactor);
                }
            }, token);

            char ch;
            ShowInfoMessage();
            while (true)
            {
                ch = Console.ReadKey().KeyChar;
                Console.WriteLine();

                if (ch == 'q')
                {
                    cancellationSource.Cancel();
                    break;
                }

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
                Console.WriteLine();
            }
            mqClient.Dispose();
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
                foreach (var queue in NewTimeSpeedFactorReceivers)
                {
                    mqClient.Send(queue, new NewTimeSpeedFactor()
                    {
                        Factor = TimeSpeedFactor
                    });
                }
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
    }
}
