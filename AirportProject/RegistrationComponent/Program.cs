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
        public int StandardFood { get; set; } = 0;
        public int VeganFood { get; set; } = 0;
        public int ChildFood { get; set; } = 0;
    }
    class Registration
    {
        public List<Flight> Flights { get; set; } = new List<Flight>();
        public List<CheckInRequest> PasList { get; set; } = new List<CheckInRequest>();
        public RabbitMqClient MqClient { get; set; } = new RabbitMqClient();
        private readonly object pasLock = new object();
        const int MIN_ERR = 1;
        const int MAX_ERR = 100;
        public double TimeCoef { get; set; } = 1;

        const string scheduleReg = Component.Schedule + Component.Registration;
        const string pasReg = Component.Passenger + Component.Registration;
        const string regPas = Component.Registration + Component.Passenger;
        const string regStorage = Component.Registration + Component.Storage;
        const string regStorageBaggage = Component.Registration + Component.Storage + Subject.Baggage;
        const string regCash = Component.Registration + Component.Cashbox;
        const string cashReg = Component.Cashbox + Component.Registration;
        const string grServReg = Component.GroundService + Component.Registration;
        const string regGrServ = Component.Registration + Component.GroundService;

        static void Main(string[] args)
        {
            var reg = new Registration();
            
            reg.MqClient.DeclareQueues(scheduleReg, pasReg, regPas, regStorage, regStorageBaggage, regCash, cashReg);

            reg.MqClient.SubscribeTo<FlightStatusUpdate>(scheduleReg, (mes) =>
            {
                reg.UpdateFlightStatus(mes.FlightId, mes.Status);
            });

            reg.MqClient.SubscribeTo<CheckInRequest>(pasReg, (mes) =>
            {
                Thread.Sleep(2000);
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
                                new CheckInResponse() { PassengerId = mes.PassengerId, Status = CheckInStatus.Registrated });
                            reg.PassToTerminal(match.PassengerId, match.FlightId, match.HasBaggage, match.FoodType);
                        }
                        else // Если билет неверный
                            reg.MqClient.Send<CheckInResponse>(regPas,
                                new CheckInResponse() { PassengerId = mes.PassengerId, Status = CheckInStatus.WrongTicket });

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
                    break;
                case FlightStatus.CheckIn:
                    Flights.Find(e => (e.FlightId == id)).Status = status;
                    break;
                case FlightStatus.Boarding:
                    var boarding = Flights.Find(e => (e.FlightId == id));
                    boarding.Status = status;
                    MqClient.Send<FoodInfoResponse>(regGrServ,
                        new FoodInfoResponse()
                        {
                            FlightId = id,
                            FoodList = new List<Tuple<Food, int>>()
                            { 
                                Tuple.Create(Food.Standard, boarding.StandardFood),
                                Tuple.Create(Food.Vegan, boarding.VeganFood),
                                Tuple.Create(Food.Child, boarding.ChildFood),
                            }
                        });
                    break;
                case FlightStatus.Departed:
                    Flights.Find(e => (e.FlightId == id)).Status = status;
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
                var errorTime = new Random().Next(MIN_ERR, MAX_ERR);
                Thread.Sleep((int) (errorTime * 100000 * TimeCoef));

                var status = Flights.Find(e => e.FlightId == flightId).Status;
                if (status == FlightStatus.Boarding || status == FlightStatus.Departed)
                {
                    MqClient.Send<CheckInResponse>(regPas,
                        new CheckInResponse() { PassengerId = passengerId, Status = CheckInStatus.LateForTerminal });
                    return;
                }
            }
            
            // Отправить пассажира в накопитель
            MqClient.Send<PassengerStoragePass>(regStorage,
                    new PassengerStoragePass() { PassengerId = passengerId, FlightId = flightId });

            if (baggage)
            {
                // Отправить багаж в накопитель - Накопитель(flightId)
                MqClient.Send<BaggageStoragePass>(regStorageBaggage,
                    new BaggageStoragePass() { FlightId = flightId });
            }

            // Добавить еду для рейса
            var flight = Flights.Find(e => e.FlightId == flightId);

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
                    break;

                case (FlightStatus.Boarding):
                    MqClient.Send<CheckInResponse>(regPas,
                    new CheckInResponse() { PassengerId = passengerId, Status = CheckInStatus.Late });
                    break;

                case (FlightStatus.CheckIn):
                    PasList.Add(new CheckInRequest()
                    { PassengerId = passengerId, FlightId = flightId, HasBaggage = hasBaggage, FoodType = foodType });
                    // Отправить запрос кассе на проверку билета
                    MqClient.Send<CheckTicketRequest>(regCash,
                    new CheckTicketRequest() { PassengerId = passengerId, FlightId = flightId });
                    break;
                default:
                    MqClient.Send<CheckInResponse>(regPas,
                    new CheckInResponse() { PassengerId = passengerId, Status = CheckInStatus.NoSuchFlight });
                    break;
        }
        }
    }
}
