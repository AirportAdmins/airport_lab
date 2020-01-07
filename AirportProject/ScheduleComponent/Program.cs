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
            var mqClient = new RabbitMqClient();

            var queueName = Component.Schedule + Component.Airplane + Subject.AirplaneTypes;
            mqClient.DeclareQueues(queueName);

            var message = new MyCustomMessage()
            {
                MyTime = DateTime.Now,
                MyString = "Hello World, This is a test",
                MyArray = new int[] { 1, 2, 3, 44, 42 }
            };

            mqClient.Send(queueName, message);

            mqClient.SubscribeTo<MyCustomMessage>(queueName, (mes) =>
            {
                Console.WriteLine("{0} Received: {1}", DateTime.Now, mes);
            });

            Console.ReadLine();
            mqClient.Dispose();
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
