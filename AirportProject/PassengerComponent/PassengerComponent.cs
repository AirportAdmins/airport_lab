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

namespace PassengerComponent
{
    class PassengerComponent
    {
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

        public const int PASSENGER_CREATION_PERIOD_MS = 60 * 1000;

        public static double timeFactor = 1.0;

        public void Start()
        {
            var mqClient = new RabbitMqClient();

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
                while (!cancellationToken.IsCancellationRequested)
                {
                    // generate passengers

                    playDelaySource.CreateToken().Sleep(PASSENGER_CREATION_PERIOD_MS);
                }
            }, cancellationToken);

            // Own run for every queue (e.g. waiting-for-response-passengers-from-cashbox)
            Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // send passengers to do something
                }
            }, cancellationToken);

            mqClient.SubscribeTo<Timetable>(TimeServiceToPassengerQueue, (mes) =>
            {

            });

            mqClient.SubscribeTo<TicketResponse>(CashboxToPassengerQueue, (mes) =>
            {
                
            });

            mqClient.SubscribeTo<CheckInResponse>(RegistrationToPassengerQueue, (mes) =>
            {

            });

            mqClient.SubscribeTo<PassengerPassMessage>(BusStorageToPassengerQueue, (mes) =>
            {

            });

            mqClient.SubscribeTo<NewTimeSpeedFactor>(TimeServiceToPassengerQueue, (mes) =>
            {
                playDelaySource.TimeFactor = mes.Factor;
            });

            Console.ReadLine();
            cancellationSource.Cancel();
            mqClient.Dispose();
        }
    }
}
