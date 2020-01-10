using System;
using System.Collections.Generic;
using System.Text;

namespace PassengerComponent
{
    class PassengerComponent
    {
        public const PassengerToCashboxQueue = Component.Passenger + Component.Cashbox;
        public const PassengerToRegistrationQueue = Component.Passenger + Component.Registration;
        public const PassengerToLogsQueue = Component.Passenger + Component.Logs; 

        public const TimetableToPassengerQueue = Component.Timetable + Component.Passenger;
        public const CashboxToPassengerQueue = Component.Cashbox + Component.Passenger;
        public const RegistrationToPassengerQueue = Component.Registration + Component.Passenger;
        public const StorageToPassengerQueue = Component.Storage + Component.Passenger;
        public const BusToPassengerQueue = Component.Bus + Component.Passenger;
        public const TimeServiceToPassengerQueue = Component.TimeService + Component.Passenger;

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

            mqClient.

        }
    }
}
