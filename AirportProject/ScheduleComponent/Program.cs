using RabbitMqWrapper;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using AirportLibrary;

namespace ScheduleComponent
{
    class Program
    {
        const string QUEUE_NAME = "schedule-cashbox";
        static void Main(string[] args)
        {
            Console.WriteLine(Queue.From(Component.Airplane).To(Component.Baggage));
            Console.WriteLine(Queue.FromAnyTo(Component.Logs));
        }
    }

    class MyCustomMessage
    {
        
    }
}
