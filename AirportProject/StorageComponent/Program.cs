using System;
using System.Collections.Generic;
using RabbitMqWrapper;
using AirportLibrary;
using AirportLibrary.DTO;
using System.Threading;
using System.Collections.Concurrent;
using AirportLibrary.Delay;

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


        public List<CheckInRequest> PasList { get; set; } = new List<CheckInRequest>();
        public RabbitMqClient MqClient { get; set; } = new RabbitMqClient();
        public PlayDelaySource DelaySource { get; set; } = new PlayDelaySource(1);
        private readonly object flightLock = new object();
        const int MIN_ERR_MS = 10000; // задержка пассажира от 10 секунд 
        const int MAX_ERR_MS = 600000; // до 10 минут игрового времени
        const int REG_TIME_MS = 5000; // регистрация - 5 секунд игрового времени
        public double TimeCoef { get; set; } = 1;

        const string timeStorage = Component.TimeService + Component.Storage;
        const string regStorage = Component.Registration + Component.Storage;
        const string regStorageBaggage = Component.Registration + Component.Storage + Subject.Baggage;
        const string busStorage = Component.Bus + Component.Storage;
        const string baggageStorage = Component.Baggage + Component.Storage;
        const string storagePas = Component.Registration + Component.Passenger + Subject.Status;
        const string storageBus = Component.Storage + Component.Bus;
        const string storageBaggage = Component.Storage + Component.Baggage;

        public static readonly List<string> queues = new List<string>
        {
            timeStorage, regStorage, regStorageBaggage, busStorage, baggageStorage, storagePas, storageBus, storageBaggage
        };

        static void Main(string[] args)
        {
            var storage = new Storage();

            storage.MqClient.DeclareQueues(queues.ToArray());
            storage.MqClient.PurgeQueues(queues.ToArray());

            storage.MqClient.SubscribeTo<NewTimeSpeedFactor>(timeStorage, (mes) =>
            {
                storage.DelaySource.TimeFactor = mes.Factor;
            });

            storage.MqClient.SubscribeTo<PassengerStoragePass>(regStorage, (mes) =>
            {
                Console.WriteLine($"Received from Registration: {mes.FlightId} - {mes.PassengerId}");
                storage.AddPassenger(mes.FlightId, mes.PassengerId);
            });

            storage.MqClient.SubscribeTo<BaggageStoragePass>(regStorageBaggage, (mes) =>
            {
                Console.WriteLine($"Received from Registration: {mes.FlightId} + 1 baggage");
                storage.AddBaggage(mes.FlightId);
            });

            storage.MqClient.SubscribeTo<PassengersFromStorageRequest>(busStorage, (mes) =>
            {
                Console.WriteLine($"Received from Bus: {mes.BusId}, {mes.FlightId}, {mes.Capacity}");
                storage.DelaySource.CreateToken().Sleep(REG_TIME_MS);
                storage.PassPassengers(mes.BusId, mes.FlightId, mes.Capacity);
            });

            // Ответ кассы
            storage.MqClient.SubscribeTo<CheckTicketResponse>(cashReg, (mes) =>
            {
                lock (storage.flightLock)
                {
                    var match = storage.PasList.Find(e => (e.PassengerId == mes.PassengerId));
                    if (match != null)
                    {
                        if (mes.HasTicket) // Если билет верный
                        {
                            storage.MqClient.Send<CheckInResponse>(regPas,
                                new CheckInResponse() { PassengerId = mes.PassengerId, Status = CheckInStatus.Registered });
                            Console.WriteLine($"Sent to Passenger: {mes.PassengerId}, {CheckInStatus.Registered}");
                            storage.PassToTerminal(match.PassengerId, match.FlightId, match.HasBaggage, match.FoodType);
                        }
                        else // Если билет неверный
                        {
                            storage.MqClient.Send<CheckInResponse>(regPas,
                                new CheckInResponse() { PassengerId = mes.PassengerId, Status = CheckInStatus.WrongTicket });
                            Console.WriteLine($"Sent to Passenger: {mes.PassengerId}, {CheckInStatus.WrongTicket}");
                        }

                        storage.PasList.Remove(match);
                    }
                }
            });
            //reg.MqClient.Dispose();
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
            var flight = Flights.Find(e => e.FlightId == flightId);
            if (flight == null)
                return;

            int count = flight.Passengers.Count;
            int passCount = (count > capacity) ? capacity : count;
            var passengers = flight.Passengers.GetRange(0, passCount);

            MqClient.Send<PassengersFromStorageResponse>(storageBus,
                new PassengersFromStorageResponse() { BusId = busId, PassengersCount = passCount, PassengersIds = passengers });
            Console.WriteLine($"Sent to Bus: {busId}, {passCount}, {passengers}");

            flight.Passengers.RemoveRange(0, passCount);
            // отклик пассажиру

            TryToRemove(flightId);
        }

        public void PassBaggage(string carId, string flightId, int capacity)
        {
            var flight = Flights.Find(e => e.FlightId == flightId);
            if (flight == null)
                return;

            int count = (flight.Baggage > capacity) ? capacity : flight.Baggage;

            MqClient.Send<BaggageFromStorageResponse>(storageBaggage,
                new BaggageFromStorageResponse() { BaggageCarId = carId, BaggageCount = count });
            Console.WriteLine($"Sent to Baggage car: {carId}, {count}");

            flight.Baggage -= count;
            // отклик пассажиру

            TryToRemove(flightId);
        }

        public void TryToRemove(string id)
        {
            var flight = Flights.Find(e => e.FlightId == id);
            if (flight.Baggage == 0 && flight.Passengers.Count == 0)
                Flights.Remove(flight);
        }
    }
}