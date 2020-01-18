using AirportLibrary;
using AirportLibrary.DTO;
using RabbitMqWrapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace TimetableComponent
{
    class TimetableComponent
    {
        public const string TimetableToPassengerQueue =
            Component.Timetable + Component.Passenger;

        public const string ScheduleToTimetableQueue =
            Component.Schedule + Component.Timetable;
        public const string TimeServiceToTimetableQueue =
            Component.TimeService + Component.Timetable;
        public const string TimeServiceToTimetableFactorQueue =
            Component.TimeService + Component.Timetable + Subject.Factor;

        public void Start()
        {
            var mqClient = new RabbitMqClient();

            mqClient.DeclareQueues(
                TimetableToPassengerQueue,
                ScheduleToTimetableQueue,
                TimeServiceToTimetableQueue
            );

            mqClient.PurgeQueues(
                TimetableToPassengerQueue,
                ScheduleToTimetableQueue,
                TimeServiceToTimetableQueue
            );

            var timetable = new ConsoleTimetable();

            mqClient.SubscribeTo<CurrentPlayTime>(TimeServiceToTimetableQueue, (mes) =>
            {
                lock (timetable)
                {
                    timetable.SetCurrentTime(mes.PlayTime);
                    timetable.Draw();
                }
            });

            mqClient.SubscribeTo<NewTimeSpeedFactor>()

            mqClient.SubscribeTo<FlightStatusUpdate>(ScheduleToTimetableQueue, (mes) =>
            {
                lock (timetable)
                {
                    // TODO if flight is departed, then remove it in N minutes
                    timetable.UpdateFlight(mes);
                    mqClient.Send(TimetableToPassengerQueue, timetable.GetTimetable());
                }
            });
        }
    }
}
