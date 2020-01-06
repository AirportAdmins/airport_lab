using System;
using MqWrapper;

namespace MqWrapperTestProject
{
    class Program
    {
        const string QUEUE_NAME = "schedule-cashbox";
        static void Main(string[] args)
        {
            var mqClient = new WrapperClient();

            mqClient.DeclareQueues(QUEUE_NAME);

            var message = new MyCustomMessage()
            {
                MyTime = DateTime.Now,
                MyString = "Hello World, This is a test",
                MyArray = new int[] { 1, 2, 3, 44, 42 }
            };

            mqClient.Send(QUEUE_NAME, message);

            mqClient.SubscribeTo<MyCustomMessage>(QUEUE_NAME, (mes) =>
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
