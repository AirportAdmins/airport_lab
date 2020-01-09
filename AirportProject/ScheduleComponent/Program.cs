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
        static void Main(string[] args)
        {
            
        }
    }

    class MyCustomMessage
    {
        public DateTime MyTime { get; set; }
        public string MyString { get; set; }
        public int[] MyArray { get; set; }

        public override string ToString()
        {
            return MyTime.ToString() + "; " + MyString + "; " + String.Join(", ", MyArray);
        }
    }
}
