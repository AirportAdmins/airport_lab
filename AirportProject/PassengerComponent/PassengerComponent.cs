using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AirportLibrary;
using RabbitMqWrapper;

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
        public const string StorageToPassengerQueue = Component.Storage + Component.Passenger;
        public const string BusToPassengerQueue = Component.Bus + Component.Passenger;
        public const string TimeServiceToPassengerQueue = Component.TimeService + Component.Passenger;

        public static readonly List<string> queues = new List<string>
        {
            PassengerToCashboxQueue,
            PassengerToRegistrationQueue,
            PassengerToLogsQueue,
            TimetableToPassengerQueue,
            CashboxToPassengerQueue,
            RegistrationToPassengerQueue,
            StorageToPassengerQueue,
            BusToPassengerQueue,
            TimeServiceToPassengerQueue
        };

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

            Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // generate passengers
                }
            }, cancellationToken);

            Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // send passengers to do something
                }
            }, cancellationToken);
        }
    }
}
