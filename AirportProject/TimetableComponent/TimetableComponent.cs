using AirportLibrary;
using AirportLibrary.DTO;
using AirportLibrary.Delay;
using RabbitMqWrapper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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

        const int TIME_TILL_REMOVING_DEPARTED_FLIGHT_MS = 15 * 60 * 1000;

        const double timeFactor = 1.0;
        PlayDelaySource source = new PlayDelaySource(timeFactor);
        

        public void Start()
        {
            var mqClient = new RabbitMqClient();

            mqClient.DeclareQueues(
                TimetableToPassengerQueue,
                ScheduleToTimetableQueue,
                TimeServiceToTimetableQueue,
				TimeServiceToTimetableFactorQueue
            );

            mqClient.PurgeQueues(
                TimetableToPassengerQueue,
                ScheduleToTimetableQueue,
                TimeServiceToTimetableQueue,
				TimeServiceToTimetableFactorQueue
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

            mqClient.SubscribeTo<NewTimeSpeedFactor>(TimeServiceToTimetableFactorQueue, (mes) =>
            {
                source.TimeFactor = mes.Factor;
            });

            mqClient.SubscribeTo<FlightStatusUpdate>(ScheduleToTimetableQueue, (mes) =>
            {
                lock (timetable)
                {
                    // TODO if flight is departed, then remove it in N minutes
                    timetable.UpdateFlight(mes);
                    mqClient.Send(TimetableToPassengerQueue, new Timetable() { Flights = timetable.GetTimetable() });
                    if (mes.Status == FlightStatus.Departed)
                    {
                        Task.Run(() =>
                        {
                            source.CreateToken().Sleep(TIME_TILL_REMOVING_DEPARTED_FLIGHT_MS);
                            lock (timetable) {
                                timetable.RemoveFlight(mes.FlightId);
                            }
                        });
                    }
                }
            });
        }
    }
}
