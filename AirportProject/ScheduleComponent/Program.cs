<<<<<<< HEAD
﻿using RabbitMqWrapper;
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
<<<<<<< HEAD
<<<<<<< HEAD
=======

>>>>>>> 7d53ec5670118ce6a98767c53196f0530aefbd67
=======
>>>>>>> c477c3d1593fd85d1504cbdb24e7fce6808b38ba
        static double timeFactor = 1.0;
        static readonly PlayDelaySource delaySource = new PlayDelaySource(timeFactor);

        const int SLEEP_PLAY_TIME_MS = 10 * 60 * 1000;
        const int NUMBER_OF_SLEEPS = 10;

        static int countOfSleepingThreads = 0;
<<<<<<< HEAD
        
<<<<<<< HEAD
=======

        const string QUEUE_NAME = "schedule-cashbox";
        const string QUEUE_NAME1 = "hello";

>>>>>>> 7d53ec5670118ce6a98767c53196f0530aefbd67
=======

>>>>>>> c477c3d1593fd85d1504cbdb24e7fce6808b38ba
        static void Main(string[] args)
        {
            var mqClient = new RabbitMqClient();

<<<<<<< HEAD
<<<<<<< HEAD
=======

>>>>>>> 7d53ec5670118ce6a98767c53196f0530aefbd67
=======
>>>>>>> c477c3d1593fd85d1504cbdb24e7fce6808b38ba
            var timeQueue = Component.TimeService + Component.Schedule;
            var factorQueue = timeQueue + "factor";
            mqClient.DeclareQueues(timeQueue, factorQueue);

<<<<<<< HEAD
            mqClient.SubscribeTo<CurrentPlayTime>(timeQueue, (mes) =>
            {
<<<<<<< HEAD
=======
            mqClient.PurgeQueues(QUEUE_NAME, QUEUE_NAME1);


            mqClient.SubscribeTo<CurrentPlayTime>(timeQueue, (mes) =>
            {

>>>>>>> 7d53ec5670118ce6a98767c53196f0530aefbd67
=======
>>>>>>> c477c3d1593fd85d1504cbdb24e7fce6808b38ba
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
<<<<<<< HEAD
=======

>>>>>>> 7d53ec5670118ce6a98767c53196f0530aefbd67
=======
>>>>>>> c477c3d1593fd85d1504cbdb24e7fce6808b38ba

            Console.ReadLine();
            mqClient.Dispose();
=======
        static void Main(string[] args)
        {

>>>>>>> 7cd70f0cea4d36b1993df4833296854c218b4a34
=======
﻿namespace ScheduleComponent
{
    class Program
    {
        static void Main(string[] args)
        {
            new ScheduleComponent().Start();
>>>>>>> 8a4d2c93d312db48d8849b1e37cd1a75ec23596c
        }
    }
}