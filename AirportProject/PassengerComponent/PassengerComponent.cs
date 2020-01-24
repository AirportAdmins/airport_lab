using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AirportLibrary;
using RabbitMqWrapper;
using PassengerComponent.Passengers;
using AirportLibrary.Delay;
using AirportLibrary.DTO;
using System.Collections.Concurrent;
using System.Linq;

namespace PassengerComponent
{
    class PassengerComponent
    {
        const string PASSENGER_PREFIX = "PS";

        public const string PassengerToCashboxQueue = Component.Passenger + Component.Cashbox;
        public const string PassengerToRegistrationQueue = Component.Passenger + Component.Registration;
        public const string PassengerToLogsQueue = Component.Passenger + Component.Logs;

        public const string TimetableToPassengerQueue = Component.Timetable + Component.Passenger;
        public const string CashboxToPassengerQueue = Component.Cashbox + Component.Passenger;
        public const string RegistrationToPassengerQueue = Component.Registration + Component.Passenger;
        public const string BusStorageToPassengerQueue = Component.Passenger + Subject.Status;
        public const string TimeServiceToPassengerQueue = Component.TimeService + Component.Passenger;

        public static readonly List<string> queues = new List<string>
        {
            PassengerToCashboxQueue,
            PassengerToRegistrationQueue,
            PassengerToLogsQueue,
            TimetableToPassengerQueue,
            CashboxToPassengerQueue,
            RegistrationToPassengerQueue,
            BusStorageToPassengerQueue,
            TimeServiceToPassengerQueue
        };

        public const int PASSENGER_CREATION_PERIOD_MS = 45 * 1000;
        public const int PASSENGER_ACTIVITY_PERIOD_MS = 60 * 1000;

        public const double CHANCE_TO_MISTAKE_CB = 0.001;
        public const double CHANCE_TO_DO_NORMAL_ACTION_CB = 0.075 + CHANCE_TO_MISTAKE_CB;
        public const double CHANCE_TO_RETURN_TICKET = 0.0025 + CHANCE_TO_MISTAKE_CB;

        public const double CHANCE_TO_MISTAKE_REG = 0.005;
        public const double CHANCE_TO_DO_NORMAL_ACTION_REG = 0.125 + CHANCE_TO_MISTAKE_REG;

        public static double timeFactor = 1.0;

        Random random = new Random();

        PassengerGenerator generator = new PassengerGenerator(PASSENGER_PREFIX);

        Timetable timetable;

        ConcurrentDictionary<string, Passenger> idlePassengers = new ConcurrentDictionary<string, Passenger>();
        ConcurrentDictionary<string, Passenger> waitingForResponsePassengers = new ConcurrentDictionary<string, Passenger>();
        ConcurrentDictionary<string, Passenger> passivePassengers = new ConcurrentDictionary<string, Passenger>();

        ConcurrentDictionary<string, FlightStatusUpdate> departedFlights = new ConcurrentDictionary<string, FlightStatusUpdate>();

        RabbitMqClient mqClient = new RabbitMqClient();

        public void Start()
        {

            mqClient.DeclareQueues(
                queues.ToArray()
            );

            mqClient.PurgeQueues(
                queues.ToArray()
            );

            var cancellationSource = new CancellationTokenSource();
            var cancellationToken = cancellationSource.Token;
            var playDelaySource = new PlayDelaySource(timeFactor);

            Task.Run(() =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var num = 4;
                        // generate passengers
                        if (random.NextDouble() < 1.0 / num)
                        {
                            var passenger = generator.GeneratePassenger();
                            Console.WriteLine($"New passenger: {passenger.PassengerId}");
                            idlePassengers.TryAdd(
                                passenger.PassengerId,
                                passenger
                            );
                        }

                        playDelaySource.CreateToken().Sleep(PASSENGER_CREATION_PERIOD_MS / num);
                    }
                } catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }, cancellationToken);

            // Own run for every queue (e.g. waiting-for-response-passengers-from-cashbox)
            Task.Run(() =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        // send passengers to do something
                        var copyPassengers = new List<Passenger>(idlePassengers.Values);
                        foreach (var passenger in copyPassengers)
                        {
                            TryToSendPassengerSomewhere(passenger);
                        }

                        playDelaySource.CreateToken().Sleep(PASSENGER_ACTIVITY_PERIOD_MS);
                    }
                } catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }, cancellationToken);

            mqClient.SubscribeTo<Timetable>(TimetableToPassengerQueue, (mes) =>
            {
                try
                {
                    Console.WriteLine("Timetable updated");
                    // Careful: do not change to "updating" of timetable, just replace it
                    // Otherwise it can be modified at the same time it's being traversed
                    timetable = mes;
                    var flights = mes.Flights.Select(f => f.FlightId);
                    foreach (var passenger in passivePassengers.Values)
                    {
                        if (!flights.Contains(passenger.FlightId))
                        {
                            if (passivePassengers.TryRemove(passenger.PassengerId, out var depPassenger))
                            {
                                Console.WriteLine($"Passenger {depPassenger.PassengerId} has departed with flight {depPassenger.FlightId}");
                            };
                        }
                    }
                } catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });

            mqClient.SubscribeTo<TicketResponse>(CashboxToPassengerQueue, (mes) =>
            {
                try
                {
                    HandleCashboxResponse(mes);
                } catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });

            mqClient.SubscribeTo<CheckInResponse>(RegistrationToPassengerQueue, (mes) =>
            {
                try { 
                    HandleRegistrationResponse(mes);
                } catch (Exception e)
                {
                    Console.WriteLine(e);
                }
        });

            mqClient.SubscribeTo<PassengerPassMessage>(BusStorageToPassengerQueue, (mes) =>
            {
                try { 
                    HandleBusStorageResponse(mes);
                } catch (Exception e)
                {
                    Console.WriteLine(e);
                }
        });

            mqClient.SubscribeTo<NewTimeSpeedFactor>(TimeServiceToPassengerQueue, (mes) =>
            {
                playDelaySource.TimeFactor = mes.Factor;
            });

            Console.ReadLine();
            cancellationSource.Cancel();
            mqClient.Dispose();
        }

        private void HandleBusStorageResponse(PassengerPassMessage mes)
        {
            var newStatus = mes.Status;
            var objId = mes.ObjectId;
            string obj;
            switch (newStatus)
            {
                case PassengerStatus.InBus:
                    obj = "bus";
                    break;
                case PassengerStatus.InAirplane:
                    obj = "plane";
                    break;
                default:
                    throw new ArgumentException($"New status cannot be of this value: {newStatus}");
            }
            foreach (var passId in mes.PassengersIds)
            {
                Console.WriteLine($"Passenger {passId} has been placed in {obj} {objId}");
                passivePassengers[passId].Status = newStatus;
            }
        }

        private void HandleRegistrationResponse(CheckInResponse mes)
        {
            var passId = mes.PassengerId;
            // if placed in terminal then he becomes completely passive
            if (mes.Status == CheckInStatus.Terminal)
            {
                if (waitingForResponsePassengers.TryRemove(passId, out var passenger))
                {
                    if (passivePassengers.TryAdd(passenger.PassengerId, passenger))
                    {
                        passenger.Status = PassengerStatus.InStorage;
                        Console.WriteLine($"Passenger {passId} has been placed in terminal");
                    }
                }
            }
            // if came early then returns back to idle state
            else if (mes.Status == CheckInStatus.Early)
            {
                if (waitingForResponsePassengers.TryRemove(passId, out var passenger))
                {
                    Console.WriteLine($"Passenger {passId} has come too early for flight {passenger.FlightId} registration");
                    if (idlePassengers.TryAdd(passenger.PassengerId, passenger))
                    {
                        Console.WriteLine($"Passenger {passId} goes away to come back later");
                    }
                }
            }
            // if registered then he waits for placing in terminal
            else if (mes.Status == CheckInStatus.Registered)
            {
                var passenger = waitingForResponsePassengers[passId];
                passenger.Status = PassengerStatus.Registered;
                Console.WriteLine($"Passenger {passId} has been registered for flight {passenger.FlightId} registration");
            }
            // else disappears 
            else
            {
                if (waitingForResponsePassengers.TryRemove(passId, out var passenger))
                {
                    var action = "";
                    switch (mes.Status)
                    {
                        case CheckInStatus.Late:
                            action = "is not really punctual";
                            break;
                        case CheckInStatus.LateForTerminal:
                            action = "loves duty free";
                            break;
                        case CheckInStatus.NoSuchFlight:
                            action = "goes away puzzled forever";
                            break;
                        case CheckInStatus.WrongTicket:
                            action = "has missed something. Police will figure it out";
                            break;
                    }
                    Console.WriteLine($"Passenger {passId} {action} {mes.Status}");
                }
            }
        }

        private void HandleCashboxResponse(TicketResponse mes)
        {
            var status = mes.Status;

            Action<string> log = (action) => {
                Console.WriteLine($"Passenger {mes.PassengerId} {action}");
            };

            // AlreadyHasTicket - go to idle
            // Late - go to idle
            // NoTicketsLeft - go to idle

            // HasTicket - change status & go to idle
            // TicketReturn - change status & go to idle

            // LateReturn - get lost
            // ReturnError - get lost
            if (waitingForResponsePassengers.TryRemove(mes.PassengerId, out var passenger))
            {
                switch (status)
                {
                    case TicketStatus.AlreadyHasTicket:
                        // TODO change something here - you can't remember both the first and the second
                        // (that attempt was made for buying) tickets for a passenger at the same time
                        // so you need change the way you store flightId (return it in message, store separately, etc.)
                        // or make algorithm generating "bad" events (like buying ticket twice) smarter (read "harder")
                        log($"already has a ticket");
                        idlePassengers.TryAdd(passenger.PassengerId, passenger);
                        break;
                    case TicketStatus.Late:
                        log($"is late for buying ticket for flight {passenger.FlightId}");
                        idlePassengers.TryAdd(passenger.PassengerId, passenger);
                        break;
                    case TicketStatus.NoTicketsLeft:
                        log($"is too late, no tickets left for flight {passenger.FlightId}");
                        idlePassengers.TryAdd(passenger.PassengerId, passenger);
                        break;
                    case TicketStatus.HasTicket:
                        log($"has bought a ticket for flight {passenger.FlightId} successfully");
                        passenger.Status = PassengerStatus.HasTicket;
                        idlePassengers.TryAdd(passenger.PassengerId, passenger);
                        break;
                    case TicketStatus.TicketReturn:
                        log($"returned a ticket for flight {passenger.FlightId} successfully");
                        passenger.Status = PassengerStatus.NoTicket;
                        idlePassengers.TryAdd(passenger.PassengerId, passenger);
                        break;
                    case TicketStatus.LateReturn:
                        log($"was late returning a ticket for flight {passenger.FlightId}");
                        break;
                    case TicketStatus.ReturnError:
                        log($"cannot return a ticket for flight {passenger.FlightId}");
                        break;
                    default:
                        break;
                }
            }
        }

        private void TryToSendPassengerSomewhere(Passenger passenger)
        {
            try
            {
                //Console.WriteLine($"Trying to send passenger {passenger.PassengerId} anywhere...");
                var tt = timetable;
                if (tt == null || tt.Flights.Count == 0)
                    return;

                var chance = random.NextDouble();
                if (passenger.Status == PassengerStatus.NoTicket)
                {
                    if (chance < CHANCE_TO_MISTAKE_CB)
                    {
                        var option = random.Next(3);
                        switch (option)
                        {
                            case 0:
                                var invalidFlights = tt.Flights.Where(
                                    f => f.Status != FlightStatus.New && f.Status != FlightStatus.CheckIn
                                ).ToList();
                                GoBuyTicketToAnyOfFlights(passenger, invalidFlights);
                                break;
                            case 1:
                                GoCheckInToAnyOfFlights(passenger, tt.Flights);
                                break;
                            case 2:
                                GoReturnTicketToAnyOfFlights(passenger, tt.Flights);
                                break;
                        }
                    }
                    else if (chance < CHANCE_TO_DO_NORMAL_ACTION_CB)
                    {
                        var goodFlights = tt.Flights.Where(
                            f => f.Status == FlightStatus.New || f.Status == FlightStatus.CheckIn
                        ).ToList();
                        GoBuyTicketToAnyOfFlights(passenger, goodFlights);
                    }
                }
                else if (passenger.Status == PassengerStatus.HasTicket)
                {
                    if (chance < CHANCE_TO_MISTAKE_REG)
                    {
                        var option = random.Next(3);
                        switch (option)
                        {
                            case 0:
                                // Important: AlreadyHasTicket should only be returned to a passenger
                                // that want to buy a ticket that he already has (see TODO above)
                                var passengersFlight = tt.Flights.Where(
                                    f => f.FlightId == passenger.FlightId
                                ).ToList();
                                GoBuyTicketToAnyOfFlights(passenger, passengersFlight);
                                break;
                            case 1:
                                var notPassengersFlights = tt.Flights.Where(
                                    f => f.FlightId != passenger.FlightId || f.Status != FlightStatus.CheckIn
                                ).ToList();
                                GoCheckInToAnyOfFlights(passenger, notPassengersFlights);
                                break;
                            case 2:
                                var invalidFlights = tt.Flights.Where(
                                    f => f.FlightId != passenger.FlightId
                                ).ToList();
                                GoReturnTicketToAnyOfFlights(passenger, invalidFlights);
                                break;
                        }
                    }
                    else if (chance < CHANCE_TO_DO_NORMAL_ACTION_REG)
                    {
                        var passengersFlight = tt.Flights.Where(
                            f => f.FlightId == passenger.FlightId && (f.Status == FlightStatus.New || f.Status == FlightStatus.CheckIn)
                        ).ToList();
                        if (passengersFlight.Count == 0)
                        {
                            // throw this passenger out
                            if (idlePassengers.TryRemove(passenger.PassengerId, out passenger))
                            {
                                Console.WriteLine($"Passenger {passenger.PassengerId} has missed check in - he sadly goes away");
                            }
                        }
                        else if (chance < CHANCE_TO_RETURN_TICKET)
                        {
                            GoReturnTicketToAnyOfFlights(passenger, passengersFlight);
                        }
                        else
                        {
                            var passFlight = passengersFlight[0];
                            if (passFlight.Status == FlightStatus.CheckIn)
                                GoCheckInToAnyOfFlights(passenger, passengersFlight);
                        }
                    }
                }
                else
                {
                    throw new ArgumentException($"Cannot send passenger with status {passenger.Status} anywhere");
                }
            } catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void GoBuyTicketToAnyOfFlights(Passenger passenger, List<FlightStatusUpdate> flights)
        {
            if (flights.Count == 0)
                return;
            var flight = flights[random.Next(0, flights.Count)];

            MovePassengerFromIdleToWaitingAndDoAction(passenger, () =>
            {
                passenger.FlightId = flight.FlightId;
                mqClient.Send(PassengerToCashboxQueue, new TicketRequest()
                {
                    PassengerId = passenger.PassengerId,
                    FlightId = flight.FlightId,
                    Action = TicketAction.Buy
                });
            },
            $"is going to buy a ticket for flight {flight.FlightId}...");
            
        }

        private void GoCheckInToAnyOfFlights(Passenger passenger, List<FlightStatusUpdate> flights)
        {
            if (flights.Count == 0)
                return;
            var flight = flights[random.Next(0, flights.Count)];

            MovePassengerFromIdleToWaitingAndDoAction(passenger, () =>
            {
                mqClient.Send(PassengerToRegistrationQueue, new CheckInRequest()
                {
                    PassengerId = passenger.PassengerId,
                    FlightId = flight.FlightId,
                    FoodType = passenger.FoodType,
                    HasBaggage = passenger.HasBaggage
                });
            },
            $"is going to check in for flight {flight.FlightId}...");
        }

        private void GoReturnTicketToAnyOfFlights(Passenger passenger, List<FlightStatusUpdate> flights)
        {
            if (flights.Count == 0)
                return;
            var flight = flights[random.Next(0, flights.Count)];

            MovePassengerFromIdleToWaitingAndDoAction(passenger, () =>
            {
                mqClient.Send(PassengerToCashboxQueue, new TicketRequest()
                {
                    PassengerId = passenger.PassengerId,
                    FlightId = flight.FlightId,
                    Action = TicketAction.Return
                });
            },
            $"is going to return a ticket for flight {flight.FlightId}...");
        }

        private void MovePassengerFromIdleToWaitingAndDoAction(Passenger passenger, Action action, string message = null)
        {
            Action<string> log = (action) => {
                Console.WriteLine($"Passenger {passenger.PassengerId} {action}");
            };
            if (idlePassengers.TryRemove(passenger.PassengerId, out passenger))
            {
                //log("was removed from idle");
                if (waitingForResponsePassengers.TryAdd(passenger.PassengerId, passenger))
                {
                    //log("was added to waiting");
                    if (message != null) log(message);
                    action();
                }
            }
        }
    }
}
