using RabbitMqWrapper;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using AirportLibrary;
using AirportLibrary.DTO;
using AirportLibrary.Delay;
using System.Threading.Tasks;

namespace ScheduleComponent
{
    class Program
    {
<<<<<<< HEAD
        static double timeFactor = 1.0;
        static readonly PlayDelaySource delaySource = new PlayDelaySource(timeFactor);

        const int SLEEP_PLAY_TIME_MS = 10 * 60 * 1000;
        const int NUMBER_OF_SLEEPS = 10;

        static int countOfSleepingThreads = 0;
        
=======
        const string QUEUE_NAME = "schedule-cashbox";
        const string QUEUE_NAME1 = "hello";
>>>>>>> 093549ecfe2a0963b4072a6b4ada80c185dd4eef
        static void Main(string[] args)
        {
            var mqClient = new RabbitMqClient();

<<<<<<< HEAD
            var timeQueue = Component.TimeService + Component.Schedule;
            var factorQueue = timeQueue + "factor";
            mqClient.DeclareQueues(timeQueue, factorQueue);
=======
            mqClient.PurgeQueues(QUEUE_NAME, QUEUE_NAME1);

            /*mqClient.DeclareQueues(QUEUE_NAME, QUEUE_NAME1);
>>>>>>> 093549ecfe2a0963b4072a6b4ada80c185dd4eef

            mqClient.SubscribeTo<CurrentPlayTime>(timeQueue, (mes) =>
            {
<<<<<<< HEAD
                //Console.WriteLine($"Queue thread id: {Thread.CurrentThread.ManagedThreadId}");
                if (countOfSleepingThreads < NUMBER_OF_SLEEPS && new Random().NextDouble() < 0.9)
                {
                    // Usage of Delay
                    // To check it working you need to run TimeServiceComponent
                    Task.Run(() =>
                    {
                        try
                        {
                            Console.WriteLine($"Task thread id: {Thread.CurrentThread.ManagedThreadId}");
                            var num = ++countOfSleepingThreads;
                            Console.WriteLine($"Thread#{num} falls asleep. Wake up at {mes.PlayTime.AddMilliseconds(SLEEP_PLAY_TIME_MS)}");
                            delaySource.CreateToken().Sleep(SLEEP_PLAY_TIME_MS);//, mes.PlayTime);
                            Console.WriteLine($"Thread#{num} wakes up");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    });
                }
            });
            mqClient.SubscribeTo<NewTimeSpeedFactor>(factorQueue, (mes) =>
            {
                Console.WriteLine("New Time Factor: {0}", mes.Factor);
                delaySource.TimeFactor = mes.Factor;
            });
=======
                MyTime = DateTime.Now,
                MyString = "Hello World, This is a test",
                MyArray = new int[] { 1, 2, 3, 44, 42 }
            };

            mqClient.Send(QUEUE_NAME, message);
            mqClient.Send(QUEUE_NAME1, message);

            mqClient.SubscribeTo<MyCustomMessage>(QUEUE_NAME, (mes) =>
            {
                Console.WriteLine("{0} Received: {1}", DateTime.Now, mes);
            });*/
>>>>>>> 093549ecfe2a0963b4072a6b4ada80c185dd4eef

            Console.ReadLine();
            mqClient.Dispose();
        }
    }
}
