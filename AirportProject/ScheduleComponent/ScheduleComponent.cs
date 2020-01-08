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
        public const string ScheduleToTimetableQueue =
            Component.Schedule + Component.Timetable;
        public const string ScheduleToCashboxQueue =
            Component.Schedule + Component.Cashbox;
        public const string ScheduleToRegistrationQueue =
            Component.Schedule + Component.Registration;
        public const string ScheduleToGroundServiceQueue =
            Component.Schedule + Component.GroundService;
        public const string ScheduleToAirplaneQueue =
            Component.Schedule + Component.Airplane;

        public const string TimeServiceToScheduleQueue =
            Component.TimeService + Component.Schedule;
        public const string GroundServiceToScheduleQueue =
            Component.GroundService + Component.Schedule;
        public const string AirplaneToScheduleQueue =
            Component.Airplane + Component.Schedule;

        public static readonly List<string> queues = new List<string>
        {
            ScheduleToTimetableQueue,
            ScheduleToCashboxQueue,
            ScheduleToRegistrationQueue,
            ScheduleToGroundServiceQueue,
            ScheduleToAirplaneQueue,
            TimeServiceToScheduleQueue,
            GroundServiceToScheduleQueue,
            AirplaneToScheduleQueue
        };

        IFlightManager flightManager;

        public void Start()
        {
            var mqClient = new RabbitMqClient();

            mqClient.DeclareQueues(
                queues.ToArray()
            );
            mqClient.PurgeQueues(
                queues.ToArray()
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
                    Console.WriteLine("Flight update.");
                    Console.WriteLine($"Id: {flight.FlightId}.");
                    Console.WriteLine($"Status: {flight.Status}.");
                    Console.WriteLine($"DepartureTime: {flight.DepartureTime}");
                    Console.WriteLine($"TicketCount: {flight.Model.Seats}");
                    Console.WriteLine();
                    mqClient.Send(
                        ScheduleToTimetableQueue,
                        statusUpdate);
                    if (statusUpdate.Status == FlightStatus.New)
                    {
                        mqClient.Send(ScheduleToAirplaneQueue, new AirplaneGenerationRequest()
                        {
                            AirplaneModelName = flight.Model.Model,
                            FlightId = flight.FlightId
                        });
                    }
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
                        mqClient.Send(ScheduleToGroundServiceQueue, new AirplaneServiceSignal()
                        {
                            FlightId = flight.FlightId,
                            PlaneId = flight.PlaneId,
                            Signal = ServiceSignal.Boarding
                        });
                    }
                    // Move to flight manager itself
                    if (statusUpdate.Status == FlightStatus.Departed)
                    {
                        //flightManager.RemoveByFlightId(flight.FlightId);
                    }
                }
                foreach (var flight in flightManager.GetFlightsToDeparture())
                {
                    Console.WriteLine("Flight to departure.");
                    Console.WriteLine($"Id: {flight.FlightId}.");
                    Console.WriteLine($"Status: {flight.Status}.");
                    Console.WriteLine($"DepartureTime: {flight.DepartureTime}");
                    Console.WriteLine($"TicketCount: {flight.Model.Seats}");
                    Console.WriteLine();
                    mqClient.Send(ScheduleToGroundServiceQueue, new AirplaneServiceSignal()
                    {
                        FlightId = flight.FlightId,
                        PlaneId = flight.PlaneId,
                        Signal = ServiceSignal.Departure
                    });
                }
            });

            mqClient.SubscribeTo<AirplaneServiceStatus>(GroundServiceToScheduleQueue, (mes) =>
            {
                flightManager.UpdateStatusByPlaneId(mes.PlaneId, mes.Status);
            });

            mqClient.SubscribeTo<AirplaneGenerationResponse>(AirplaneToScheduleQueue, (mes) =>
            {
                flightManager.SetPlaneForFlight(mes.FlightId, mes.PlaneId);
            });

            Console.ReadLine();
            mqClient.Dispose();
        }
    }
}
