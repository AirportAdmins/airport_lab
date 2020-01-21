using RabbitMqWrapper;
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
=======

>>>>>>> 7d53ec5670118ce6a98767c53196f0530aefbd67
        static double timeFactor = 1.0;
        static readonly PlayDelaySource delaySource = new PlayDelaySource(timeFactor);

        const int SLEEP_PLAY_TIME_MS = 10 * 60 * 1000;
        const int NUMBER_OF_SLEEPS = 10;

        static int countOfSleepingThreads = 0;
        
<<<<<<< HEAD
=======

        const string QUEUE_NAME = "schedule-cashbox";
        const string QUEUE_NAME1 = "hello";

>>>>>>> 7d53ec5670118ce6a98767c53196f0530aefbd67
        static void Main(string[] args)
        {
            var mqClient = new RabbitMqClient();

<<<<<<< HEAD
=======

>>>>>>> 7d53ec5670118ce6a98767c53196f0530aefbd67
            var timeQueue = Component.TimeService + Component.Schedule;
            var factorQueue = timeQueue + "factor";
            mqClient.DeclareQueues(timeQueue, factorQueue);

<<<<<<< HEAD
            mqClient.SubscribeTo<CurrentPlayTime>(timeQueue, (mes) =>
            {
=======
            mqClient.PurgeQueues(QUEUE_NAME, QUEUE_NAME1);


            mqClient.SubscribeTo<CurrentPlayTime>(timeQueue, (mes) =>
            {

>>>>>>> 7d53ec5670118ce6a98767c53196f0530aefbd67
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
<<<<<<< HEAD
=======

>>>>>>> 7d53ec5670118ce6a98767c53196f0530aefbd67

            Console.ReadLine();
            mqClient.Dispose();
        }
    }
}
