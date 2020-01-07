using RabbitMqWrapper;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using AirportLibrary;
using AirportLibrary.DTO;

namespace ScheduleComponent
{
    class Program
    {
        
        static void Main(string[] args)
        {
            new ScheduleComponent().Start();
        }
    }
}
