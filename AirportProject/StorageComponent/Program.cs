using System;
using System.Collections.Generic;
using RabbitMqWrapper;
using AirportLibrary;
using AirportLibrary.DTO;
using System.Threading;
using System.Collections.Concurrent;
using AirportLibrary.Delay;
using System.Threading.Tasks;

namespace StorageComponent
{
    class Flight
    {
        public string FlightId { get; set; }
        public List<string> Passengers { get; set; } = new List<string>();
        public int Baggage { get; set; } = 0;
    }
    class Storage
    {
        public List<Flight> Flights { get; set; } = new List<Flight>();
        public RabbitMqClient MqClient { get; set; } = new RabbitMqClient();
        public PlayDelaySource DelaySource { get; set; } = new PlayDelaySource(1);
        private readonly object flightLock = new object();

        const int PASS_TIME_MS = 2 * 60 * 1000; // передача пассажиров - 2 минуты игрового времени
        const int BAGGAGE_TIME_MS = 60 * 1000; // передача пассажиров - минута игрового времени

        const string timeStorage = Component.TimeService + Component.Storage;
        const string regStorage = Component.Registration + Component.Storage;
        const string regStorageBaggage = Component.Registration + Component.Storage + Subject.Baggage;
        const string busStorage = Component.Bus + Component.Storage;
        const string baggageStorage = Component.Baggage + Component.Storage;
        const string storagePas = Component.Passenger + Subject.Status;
        const string storageBus = Component.Storage + Component.Bus;
        const string storageBaggage = Component.Storage + Component.Baggage;

        ConcurrentQueue<PassengersFromStorageRequest> passengersRequests = new ConcurrentQueue<PassengersFromStorageRequest>();
        AutoResetEvent passengersResetEvent = new AutoResetEvent(false);

        ConcurrentQueue<BaggageFromStorageRequest> baggageRequests = new ConcurrentQueue<BaggageFromStorageRequest>();
        AutoResetEvent baggageResetEvent = new AutoResetEvent(false);

        public static readonly List<string> queues = new List<string>
        {
            timeStorage, regStorage, regStorageBaggage, busStorage, baggageStorage, storagePas, storageBus, storageBaggage
        };

        static void Main(string[] args)
        {
            new Storage().Start();
        }

        public void Start()
        {
            MqClient.DeclareQueues(queues.ToArray());
            MqClient.PurgeQueues(queues.ToArray());

            Task.Run(() =>
            {
                while (true)
                {
                    while (passengersRequests.TryDequeue(out var request))
                    {
                        DelaySource.CreateToken().Sleep(PASS_TIME_MS);
                        PassPassengers(request.BusId, request.FlightId, request.Capacity);
                    }
                    passengersResetEvent.WaitOne();
                }
            });

            Task.Run(() =>
            {
                while (true)
                {
                    while (baggageRequests.TryDequeue(out var request))
                    {
                        DelaySource.CreateToken().Sleep(BAGGAGE_TIME_MS);
                        PassBaggage(request.CarId, request.FlightId, request.Capacity);
                    }
                    baggageResetEvent.WaitOne();
                }
            });

            MqClient.SubscribeTo<NewTimeSpeedFactor>(timeStorage, (mes) =>
            {
                DelaySource.TimeFactor = mes.Factor;
            });

            MqClient.SubscribeTo<PassengerStoragePass>(regStorage, (mes) =>
            {
                Console.WriteLine($"Received from Registration: {mes.FlightId} - {mes.PassengerId}");
                AddPassenger(mes.FlightId, mes.PassengerId);
            });

            MqClient.SubscribeTo<BaggageStoragePass>(regStorageBaggage, (mes) =>
            {
                Console.WriteLine($"Received from Registration: {mes.FlightId} + 1 baggage");
                AddBaggage(mes.FlightId);
            });

            MqClient.SubscribeTo<PassengersFromStorageRequest>(busStorage, (mes) =>
            {
                Console.WriteLine($"Received from Bus: {mes.BusId}, {mes.FlightId}, {mes.Capacity}");
                passengersRequests.Enqueue(mes);
                passengersResetEvent.Set();
            });

            MqClient.SubscribeTo<BaggageFromStorageRequest>(baggageStorage, (mes) =>
            {
                Console.WriteLine($"Received from Baggage car: {mes.CarId}, {mes.FlightId}, {mes.Capacity}");
                baggageRequests.Enqueue(mes);
                baggageResetEvent.Set();
            });

        }

        public void AddPassenger(string flightId, string pasId)
        {
            lock (flightLock)
            {
                var flight = Flights.Find(e => e.FlightId == flightId);
                if (flight != null)
                {
                    flight.Passengers.Add(pasId);
                    return;
                }
                var newFlight = new Flight() { FlightId = flightId };
                newFlight.Passengers.Add(pasId);
                Flights.Add(newFlight);
            }
        }

        public void AddBaggage(string flightId)
        {
            lock (flightLock)
            {
                var flight = Flights.Find(e => e.FlightId == flightId);
                if (flight != null)
                {
                    flight.Baggage++;
                    return;
                }
                Flights.Add(new Flight() { FlightId = flightId, Baggage = 1 });
            }            
        }
        
        public void PassPassengers(string busId, string flightId, int capacity)
        {
            lock (flightLock)
            {
                var flight = Flights.Find(e => e.FlightId == flightId);
                if (flight == null)
                    return;

                int count = flight.Passengers.Count;
                int passCount = (count > capacity) ? capacity : count;
                var passengers = flight.Passengers.GetRange(0, passCount);

                MqClient.Send<PassengersFromStorageResponse>(storageBus,
                    new PassengersFromStorageResponse() { BusId = busId, PassengersCount = passCount, PassengersIds = passengers });
                Console.WriteLine($"Sent to Bus: {busId}, {passCount}");

                flight.Passengers.RemoveRange(0, passCount);

                MqClient.Send<PassengerPassMessage>(storagePas,
                   new PassengerPassMessage() { ObjectId = busId, Status = PassengerStatus.InBus, PassengersIds = passengers });
                Console.WriteLine($"Sent to Passenger: in {busId}");

                TryToRemove(flightId);
            }
        }

        public void PassBaggage(string carId, string flightId, int capacity)
        {
            lock (flightLock)
            {
                var flight = Flights.Find(e => e.FlightId == flightId);
                if (flight == null)
                    return;

                int count = (flight.Baggage > capacity) ? capacity : flight.Baggage;

                MqClient.Send<BaggageFromStorageResponse>(storageBaggage,
                    new BaggageFromStorageResponse() { BaggageCarId = carId, BaggageCount = count });
                Console.WriteLine($"Sent to Baggage car: {carId}, {count}");

                flight.Baggage -= count;

                TryToRemove(flightId);
            }
        }

        public void TryToRemove(string id)
        {
            lock (flightLock)
            {
                var flight = Flights.Find(e => e.FlightId == id);
                if (flight.Baggage == 0 && flight.Passengers.Count == 0)
                    Flights.Remove(flight);
            }
        }
    }
}