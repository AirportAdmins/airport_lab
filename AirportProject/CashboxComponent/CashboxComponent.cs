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

        const int TICKET_REQUEST_HANDLING_TIME_MS = 25 * 1000;
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
                        delaySource.CreateToken().Sleep(TICKET_REQUEST_HANDLING_TIME_MS);


                    }
                }

            });

            mqClient.SubscribeTo<FlightStatusUpdate>(ScheduleToCashboxQueue, mes =>
            {

            });

            mqClient.SubscribeTo<TicketRequest>(PassengerToCashboxQueue, mes =>
            {
                ticketRequests.Enqueue(mes);
                resetEvent.Set();
            });

            mqClient.SubscribeTo<CheckTicketRequest>(RegistrationToCashboxQueue, mes =>
            {

            });

            mqClient.SubscribeTo<NewTimeSpeedFactor>(TimeServiceToCashboxQueue, mes =>
            {
                delaySource.TimeFactor = mes.Factor;
            });
        }
    }
}
