using AirportLibrary;
using RabbitMqWrapper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ScheduleComponent
{
    class ScheduleComponent
    {
        public static double TimeSpeedFactor = 1.0;
        public const int TIME_FREQUENCY_MS = 100;
        public const int MAX_PERIOD_BETWEEN_FLIGHTS_MS = 60 * 60 * 1000;
        public const int MIN_PERIOD_BETWEEN_FLIGHTS_MS = 20 * 60 * 1000;

        int currentTimeBetweenFlights = 0;

        public const string ScheduleToTimetableQueue =
            Component.Schedule + Component.Timetable;
        public const string ScheduleToCashboxQueue =
            Component.Schedule + Component.Cashbox;
        public const string ScheduleToRegistrationQueue =
            Component.Schedule + Component.Registration;
        public const string TimeServiceToScheduleQueue =
            Component.TimeService + Component.Schedule;

        IFlightManager flightManager;

        public void Start()
        {
            var mqClient = new RabbitMqClient();

            mqClient.DeclareQueues(
                ScheduleToTimetableQueue,
                ScheduleToCashboxQueue,
                ScheduleToRegistrationQueue
            );

            mqClient.Send(null, new Object());

            mqClient.SubscribeTo<object>(null, (mes) =>
            {
                Console.WriteLine("{0} Received: {1}", DateTime.Now, mes);
            });

            Console.ReadLine();
            mqClient.Dispose();
        }
    }
}
