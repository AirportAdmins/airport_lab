using AirportLibrary;
using AirportLibrary.DTO;
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
        public const string ScheduleToGroundService =
            Component.Schedule + Component.GroundService;

        public const string TimeServiceToScheduleQueue =
            Component.TimeService + Component.Schedule;
        public const string GroundServiceToScheduleQueue =
            Component.GroundService + Component.Schedule;

        IFlightManager flightManager;

        public void Start()
        {
            var mqClient = new RabbitMqClient();

            mqClient.DeclareQueues(
                ScheduleToTimetableQueue,
                ScheduleToCashboxQueue,
                ScheduleToRegistrationQueue,
                TimeServiceToScheduleQueue
            );

            mqClient.SubscribeTo<CurrentPlayTime>(TimeServiceToScheduleQueue, (mes) =>
            {
                flightManager.SetCurrentTime(mes.PlayTime);
                foreach (var flight in flightManager.GetFlightChanges())
                {
                    var statusUpdate = new FlightStatusUpdate()
                    {
                        FlightId = flight.FlightId,
                        Status = flight.Status,
                        DepartureTime = flight.DepartureTime,
                        TicketCount = flight.Model.Seats
                    };
                    mqClient.Send(
                        ScheduleToTimetableQueue,
                        statusUpdate);
                    if (statusUpdate.Status != FlightStatus.Delayed)
                    {
                        mqClient.Send(ScheduleToRegistrationQueue, statusUpdate);
                        if (statusUpdate.Status != FlightStatus.CheckIn)
                        {
                            mqClient.Send(ScheduleToCashboxQueue, statusUpdate);
                        }
                    }
                    if (statusUpdate.Status == FlightStatus.Boarding)
                    {
                        var serviceSignal = new AirplaneServiceSignal()
                        {
                            FlightId = flight.FlightId,
                            PlaneId = flight.PlaneId,
                            Signal = ServiceSignal.Boarding
                        };
                        mqClient.Send(ScheduleToGroundService, serviceSignal);
                    }
                    if (statusUpdate.Status == FlightStatus.Departed)
                    {
                        //flightManager.RemoveByFlightId(flight.FlightId);
                    }
                }
            });

            mqClient.SubscribeTo<AirplaneServiceStatus>(GroundServiceToScheduleQueue, (mes) =>
            {
                //flightManager.UpdateStatusByPlaneId(mes.PlaneId, mes.Status);
            });

            Console.ReadLine();
            mqClient.Dispose();
        }
    }
}
