using AirportLibrary;
using AirportLibrary.DTO;
using RabbitMqWrapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        ConcurrentStack<DateTime> timeMessages = new ConcurrentStack<DateTime>();
        AutoResetEvent waitHandle = new AutoResetEvent(false);

        IFlightManager flightManager = new FlightManager();

        public void Start()
        {
            var mqClient = new RabbitMqClient();

            mqClient.DeclareQueues(
                queues.ToArray()
            );
            mqClient.PurgeQueues(
                queues.ToArray()
            );

            bool isFinished = false;

            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        waitHandle.WaitOne();
                        if (isFinished)
                            break;
                        DateTime playTime;
                        lock (timeMessages)
                        {
                            if (!timeMessages.TryPop(out playTime))
                                continue;
                            timeMessages.Clear();
                        }
                        // DEBUG INFO
                        //Console.WriteLine($"{playTime} Start handling: {DateTime.Now.Millisecond}:{DateTime.Now.Ticks % 10000}");
                        var datetime = DateTime.Now;
                        //Console.WriteLine($"Handled Time: {playTime} Current system: {datetime}");
                        flightManager.SetCurrentTime(playTime);
                        //Thread.Sleep(1000);
                        foreach (var flight in flightManager.GetFlightChanges())
                        {
                            var statusUpdate = new FlightStatusUpdate()
                            {
                                FlightId = flight.FlightId,
                                Status = flight.Status,
                                DepartureTime = flight.DepartureTime,
                                TicketCount = flight.Model.Seats
                            };
                            
                            Console.WriteLine($"Flight update. Current system: {datetime}");
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
                    } catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    // DEBUG INFO
                    //Console.WriteLine($"{playTime}   End handling: {DateTime.Now.Millisecond}:{DateTime.Now.Ticks % 10000}");
                }
            });

            mqClient.SubscribeTo<CurrentPlayTime>(TimeServiceToScheduleQueue, (mes) =>
            {
                // DEBUG INFO
                //Console.WriteLine($"{mes.PlayTime} Start handling: {DateTime.Now.Millisecond}:{DateTime.Now.Ticks % 10000}");
                Console.WriteLine($"Received Time: {mes.PlayTime}");
                timeMessages.Push(mes.PlayTime);
                waitHandle.Set();
                // DEBUG INFO
                //Console.WriteLine($"{mes.PlayTime}   End handling: {DateTime.Now.Millisecond}:{DateTime.Now.Ticks % 10000}");
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
            isFinished = true;
            waitHandle.Set();
        }
    }
}
