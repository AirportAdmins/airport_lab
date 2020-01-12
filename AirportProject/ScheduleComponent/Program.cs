using RabbitMqWrapper;
using System;
using System.Text;
using System.Threading;
using AirportLibrary;

namespace ScheduleComponent
{
    class Program
    {
        const string QUEUE_NAME = "schedule-cashbox";
        const string QUEUE_NAME1 = "hello";
        static void Main(string[] args)
        {
            var mqClient = new RabbitMqClient();

            mqClient.PurgeQueues(QUEUE_NAME, QUEUE_NAME1);

            /*mqClient.DeclareQueues(QUEUE_NAME, QUEUE_NAME1);

            var message = new MyCustomMessage()
            {
                MyTime = DateTime.Now,
                MyString = "Hello World, This is a test",
                MyArray = new int[] { 1, 2, 3, 44, 42 }
            };

            mqClient.Send(QUEUE_NAME, message);
            mqClient.Send(QUEUE_NAME1, message);

            mqClient.SubscribeTo<MyCustomMessage>(queueName, (mes) =>
            {
                Console.WriteLine("{0} Received: {1}", DateTime.Now, mes);
            });*/

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
