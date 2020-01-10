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
            Component.Timetable + Component.Passenger;
        public const string TimeServiceToTimetableQueue =
            Component.TimeService + Component.Passenger;

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
                timetable.SetCurrentTime(mes.PlayTime);
                timetable.Draw();
            });

            mqClient.SubscribeTo<FlightStatusUpdate>(ScheduleToTimetableQueue, (mes) =>
            {
                timetable.UpdateFlight(mes);

            });
        }
    }
}
