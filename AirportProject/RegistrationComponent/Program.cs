using System;
using System.Collections.Generic;
using RabbitMqWrapper;
using AirportLibrary;
using AirportLibrary.DTO;
using System.Threading;
using System.Collections.Concurrent;

namespace RegistrationComponent
{
    class Flight
    {
        public string FlightId { get; set; }
        public FlightStatus Status { get; set; }
        public int PasCount { get; set; } = 0;
        public int BagCount { get; set; } = 0;
        public int StandardFood { get; set; } = 0;
        public int VeganFood { get; set; } = 0;
        public int ChildFood { get; set; } = 0;
    }
    class Registration
    {
        public List<Flight> Flights { get; set; } = new List<Flight>();
        public List<CheckInRequest> PasList { get; set; } = new List<CheckInRequest>();
        public RabbitMqClient MqClient { get; set; } = new RabbitMqClient("v174153.hosted-by-vdsina.ru", "registration", "166e7343t");
        private readonly object pasLock = new object();
        const int MIN_ERR_10000 = 1; // задержка от 10 секунд
        const int MAX_ERR_10000 = 60; // до 10 минут
        const int REG_TIME_1000 = 2;
        public double TimeCoef { get; set; } = 1;

        const string timeReg = Component.TimeService + Component.Registration;
        const string scheduleReg = Component.Schedule + Component.Registration;
        const string pasReg = Component.Passenger + Component.Registration;
        const string regPas = Component.Registration + Component.Passenger;
        const string regStorage = Component.Registration + Component.Storage;
        const string regStorageBaggage = Component.Registration + Component.Storage + Subject.Baggage;
        const string regCash = Component.Registration + Component.Cashbox;
        const string cashReg = Component.Cashbox + Component.Registration;
        const string regGrServ = Component.Registration + Component.GroundService;

        public static readonly List<string> queues = new List<string>
        {
            timeReg, scheduleReg, pasReg, regPas, regStorage, regStorageBaggage, regCash, cashReg, regGrServ
        };

        static void Main(string[] args)
        {
            var reg = new Registration();
            
            reg.MqClient.DeclareQueues(queues.ToArray());
            reg.MqClient.PurgeQueues(queues.ToArray());

            reg.MqClient.SubscribeTo<NewTimeSpeedFactor>(timeReg, (mes) =>
            {
                reg.TimeCoef = mes.Factor;
            });

            reg.MqClient.SubscribeTo<FlightStatusUpdate>(scheduleReg, (mes) =>
            {
                Console.WriteLine($"Received from Schedule: {mes.FlightId} - {mes.Status}");
                reg.UpdateFlightStatus(mes.FlightId, mes.Status);
            });

            reg.MqClient.SubscribeTo<CheckInRequest>(pasReg, (mes) =>
            {
                Console.WriteLine($"Received from Passenger: {mes.PassengerId}, {mes.FlightId}, {mes.HasBaggage}, {mes.FoodType}");
                Thread.Sleep((int) (REG_TIME_1000 * 1000 / reg.TimeCoef));
                reg.Registrate(mes.PassengerId, mes.FlightId, mes.HasBaggage, mes.FoodType);
            });

            // Ответ кассы
            reg.MqClient.SubscribeTo<CheckTicketResponse>(cashReg, (mes) =>
            {
                lock (reg.pasLock)
                {
                    var match = reg.PasList.Find(e => (e.PassengerId == mes.PassengerId));
                    if (match != null)
                    {
                        if (mes.HasTicket) // Если билет верный
                        {
                            reg.MqClient.Send<CheckInResponse>(regPas,
                                new CheckInResponse() { PassengerId = mes.PassengerId, Status = CheckInStatus.Registered });
                            Console.WriteLine($"Sent to Passenger: {mes.PassengerId}, {CheckInStatus.Registered}");
                            reg.PassToTerminal(match.PassengerId, match.FlightId, match.HasBaggage, match.FoodType);
                        }
                        else // Если билет неверный
                        {
                            reg.MqClient.Send<CheckInResponse>(regPas,
                                new CheckInResponse() { PassengerId = mes.PassengerId, Status = CheckInStatus.WrongTicket });
                            Console.WriteLine($"Sent to Passenger: {mes.PassengerId}, {CheckInStatus.WrongTicket}");
                        }

                        reg.PasList.Remove(match);
                    }
                }
            });

            //reg.MqClient.Dispose();
        }

        public void UpdateFlightStatus(string id, FlightStatus status)
        {
            switch (status)
            {
                case FlightStatus.New:
                    Flights.Add(new Flight() { FlightId = id, Status = status });
                    Console.WriteLine($"Added new flight {id}");
                    break;
                case FlightStatus.CheckIn:
                    Flights.Find(e => (e.FlightId == id)).Status = status;
                    Console.WriteLine($"Added check-in: {id} - {status}");
                    break;
                case FlightStatus.Boarding:
                    var boarding = Flights.Find(e => (e.FlightId == id));
                    boarding.Status = status;
                    Console.WriteLine($"Added boarding: {id} - {status}");
                    MqClient.Send<FlightInfo>(regGrServ,
                        new FlightInfo()
                        {
                            FlightId = id,
                            PassengerCount = boarding.PasCount,
                            BaggageCount = boarding.BagCount,
                            FoodList = new List<Tuple<Food, int>>()
                            {
                                Tuple.Create(Food.Standard, boarding.StandardFood),
                                Tuple.Create(Food.Vegan, boarding.VeganFood),
                                Tuple.Create(Food.Child, boarding.ChildFood),
                            }
                        });
                    Console.WriteLine($"Sent to Ground Service: {id}, {boarding.PasCount}, {boarding.BagCount}, {boarding.StandardFood}, {boarding.VeganFood}, {boarding.ChildFood}");
                    break;
                case FlightStatus.Departed:
                    Flights.Find(e => (e.FlightId == id)).Status = status;
                    Console.WriteLine($"Added departed: {id} - {status}");
                    break;
                default:
                    break;
            }
        }

        public void PassToTerminal(string passengerId, string flightId, bool baggage, Food food)
        {
            var rand = new Random().Next(1, 10);
            if (rand < 3)
            {
                var errorTime = new Random().Next(MIN_ERR_10000, MAX_ERR_10000);
                Thread.Sleep((int) (errorTime * 10000 / TimeCoef));

                var status = Flights.Find(e => e.FlightId == flightId).Status;
                if (status == FlightStatus.Boarding || status == FlightStatus.Departed)
                {
                    MqClient.Send<CheckInResponse>(regPas,
                        new CheckInResponse() { PassengerId = passengerId, Status = CheckInStatus.LateForTerminal });
                    Console.WriteLine($"Sent to Passenger: {passengerId}, {CheckInStatus.LateForTerminal}");
                    return;
                }
            }

            var flight = Flights.Find(e => e.FlightId == flightId);

            // Отправить пассажира в накопитель
            MqClient.Send<PassengerStoragePass>(regStorage,
                    new PassengerStoragePass() { PassengerId = passengerId, FlightId = flightId });
            flight.PasCount++;

            if (baggage)
            {
                // Отправить багаж в накопитель - Накопитель(flightId)
                MqClient.Send<BaggageStoragePass>(regStorageBaggage,
                    new BaggageStoragePass() { FlightId = flightId });
                flight.BagCount++;
            }

            // Добавить еду для рейса
            switch (food)
            {
                case Food.Standard:
                    flight.StandardFood++;
                    break;
                case Food.Vegan:
                    flight.VeganFood++;
                    break;
                case Food.Child:
                    flight.ChildFood++;
                    break;
                default:
                    break;
            }
        }

        public void Registrate(string passengerId, string flightId, bool hasBaggage, Food foodType)
        {
            switch(Flights.Find(e => e.FlightId == flightId).Status)
            {
                case (FlightStatus.New):
                    MqClient.Send<CheckInResponse>(regPas,
                    new CheckInResponse() { PassengerId = passengerId, Status = CheckInStatus.Early });
                    Console.WriteLine($"Sent to Passenger: {passengerId}, {CheckInStatus.Early}");
                    break;

                case (FlightStatus.Boarding):
                    MqClient.Send<CheckInResponse>(regPas,
                    new CheckInResponse() { PassengerId = passengerId, Status = CheckInStatus.Late });
                    Console.WriteLine($"Sent to Passenger: {passengerId}, {CheckInStatus.Late}");
                    break;

                case (FlightStatus.CheckIn):
                    PasList.Add(new CheckInRequest()
                    { PassengerId = passengerId, FlightId = flightId, HasBaggage = hasBaggage, FoodType = foodType });
                    // Отправить запрос кассе на проверку билета
                    MqClient.Send<CheckTicketRequest>(regCash,
                    new CheckTicketRequest() { PassengerId = passengerId, FlightId = flightId });
                    Console.WriteLine($"Sent to CashBox: {passengerId}, {flightId}");
                    break;
                default:
                    MqClient.Send<CheckInResponse>(regPas,
                    new CheckInResponse() { PassengerId = passengerId, Status = CheckInStatus.NoSuchFlight });
                    Console.WriteLine($"Sent to Passenger: {passengerId}, {CheckInStatus.NoSuchFlight}");
                    break;
        }
        }
    }
}
