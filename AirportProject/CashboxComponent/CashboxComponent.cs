using AirportLibrary;
using AirportLibrary.Delay;
using AirportLibrary.DTO;
using RabbitMqWrapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CashboxComponent
{
    class CashboxComponent
    {
        RabbitMqClient mqClient = new RabbitMqClient();

        const string ScheduleToCashboxQueue = Component.Schedule + Component.Cashbox;
        const string PassengerToCashboxQueue = Component.Passenger + Component.Cashbox;
        const string RegistrationToCashboxQueue = Component.Registration + Component.Cashbox;
        const string TimeServiceToCashboxQueue = Component.TimeService + Component.Cashbox;

        const string CashboxToPassengerQueue = Component.Cashbox + Component.Passenger;
        const string CashboxToRegistrationQueue = Component.Cashbox + Component.Registration;
        const string CashboxToLogsQueue = Component.Cashbox + Component.Logs;

        ConcurrentQueue<TicketRequest> ticketRequests = new ConcurrentQueue<TicketRequest>();

        Dictionary<string, FlightStatusUpdate> flights = new Dictionary<string, FlightStatusUpdate>();
        Dictionary<string, string> passengerToFlight = new Dictionary<string, string>();

        const int TICKET_REQUEST_HANDLING_TIME_MS = 20 * 1000;
        const double TIME_FACTOR = 1.0;
        PlayDelaySource delaySource = new PlayDelaySource(TIME_FACTOR);

        AutoResetEvent resetEvent = new AutoResetEvent(false);

        List<string> queues = new List<string>()
        {
            ScheduleToCashboxQueue,
            PassengerToCashboxQueue,
            RegistrationToCashboxQueue,
            TimeServiceToCashboxQueue,
            CashboxToPassengerQueue,
            CashboxToRegistrationQueue,
            CashboxToLogsQueue
        };

        public void Start()
        {
            mqClient.DeclareQueues(queues.ToArray());
            mqClient.PurgeQueues(queues.ToArray());

            Task.Run(() => {

                while (true)
                {
                    resetEvent.WaitOne();

                    while (ticketRequests.TryDequeue(out var ticketRequest))
                    {
                        HandleTicketRequest(ticketRequest);
                    }
                }

            });

            mqClient.SubscribeTo<FlightStatusUpdate>(ScheduleToCashboxQueue, mes =>
            {
                lock (flights)
                {
                    if (flights.ContainsKey(mes.FlightId))
                    {
                        flights[mes.FlightId].Status = mes.Status;
                    } else
                    {
                        flights.Add(mes.FlightId, mes);
                        Console.WriteLine("New flight {0}. TicketCount: {2}", mes.FlightId, mes.TicketCount);
                    }
                }
            });

            mqClient.SubscribeTo<TicketRequest>(PassengerToCashboxQueue, mes =>
            {
                ticketRequests.Enqueue(mes);
                resetEvent.Set();
            });

            mqClient.SubscribeTo<CheckTicketRequest>(RegistrationToCashboxQueue, mes =>
            {
                HandleCheckTicketRequest(mes);
            });

            mqClient.SubscribeTo<NewTimeSpeedFactor>(TimeServiceToCashboxQueue, mes =>
            {
                delaySource.TimeFactor = mes.Factor;
            });
        }

        public void HandleTicketRequest(TicketRequest request)
        {
            delaySource.CreateToken().Sleep(TICKET_REQUEST_HANDLING_TIME_MS);

            var passId = request.PassengerId;
            var flightId = request.FlightId;
            TicketStatus status;

            lock (passengerToFlight)
            {
                lock (flights)
                {
                    if (!flights.ContainsKey(request.FlightId))
                    {
                        status = 
                            request.Action == TicketAction.Buy ? TicketStatus.Late : 
                            request.Action == TicketAction.Return ? TicketStatus.LateReturn : 
                            (TicketStatus) 420;
                    }
                    else
                    {
                        switch (request.Action)
                        {
                            case TicketAction.Buy:
                                if (passengerToFlight.ContainsKey(passId))
                                {
                                    status = TicketStatus.AlreadyHasTicket;
                                }
                                else
                                {
                                    var lastUpdate = flights[flightId];
                                    if (lastUpdate.Status == FlightStatus.New
                                        || lastUpdate.Status == FlightStatus.CheckIn)
                                    {
                                        if (lastUpdate.TicketCount > 0)
                                        {
                                            Console.WriteLine("{0} is buying a ticket. TicketCount for {1}: {2}", passId, flightId, lastUpdate.TicketCount);
                                            lastUpdate.TicketCount--;
                                            passengerToFlight.Add(passId, flightId);
                                            status = TicketStatus.HasTicket;
                                        } else
                                        {
                                            status = TicketStatus.NoTicketsLeft;
                                        }
                                    }
                                    else
                                    {
                                        status = TicketStatus.Late;
                                    }
                                }
                                break;
                            case TicketAction.Return:
                                if (passengerToFlight.TryGetValue(passId, out var passFlightId) 
                                    && passFlightId == flightId)
                                {
                                    var lastUpdate = flights[flightId];
                                    if (lastUpdate.Status == FlightStatus.New
                                        || lastUpdate.Status == FlightStatus.CheckIn)
                                    {
                                        Console.WriteLine("{0} is returning a ticket. TicketCount for {1}: {2}", passId, flightId, lastUpdate.TicketCount);
                                        passengerToFlight.Remove(passId);
                                        flights[flightId].TicketCount++;
                                        status = TicketStatus.TicketReturn;
                                    } else
                                    {
                                        status = TicketStatus.LateReturn;
                                    }
                                } else
                                {
                                    status = TicketStatus.ReturnError;
                                }
                                break;
                            default:
                                var message = $"Unknown action of {nameof(request)}.Action: {request.Action}";
                                Console.WriteLine(message);
                                throw new Exception(message);
                        }
                    }
                }
            }

            Console.WriteLine($"Passenger {passId} gets {status} for flight {flightId}");
            mqClient.Send(
                CashboxToPassengerQueue, 
                new TicketResponse()
                {
                    PassengerId = request.PassengerId,
                    Status = status
                }
            );

        }

        private void HandleCheckTicketRequest(CheckTicketRequest mes)
        {
            lock (passengerToFlight)
            {
                mqClient.Send(
                    CashboxToRegistrationQueue, 
                    new CheckTicketResponse()
                    {
                        PassengerId = mes.PassengerId,
                        HasTicket = passengerToFlight.TryGetValue(mes.PassengerId, out var flightId) 
                                && flightId == mes.FlightId
                    }
                );
            }
        }
    }
}
