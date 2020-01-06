using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace TimetableComponent
{
    class Program
    {
        static void Main(string[] args)
        {
            new TimetableComponent().Start();
        }
    }
}
