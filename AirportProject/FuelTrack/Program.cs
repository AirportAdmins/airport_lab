﻿using System;
using System.Collections.Generic;
using System.Text;





namespace FuelTruck
{
    class Program
    {
        static void Main(string[] args)
        {
            Task[] tasks = new Task[4];
            Dictionary<int, Task> tasks_ = new Dictionary<int, Task>();
            tasks_.Add(1, tasks[0]);
            tasks[0] = new Task(() => Console.WriteLine("Hi"));
            //new FuelTruck().Start();
            //ft.mqClient.Dispose();
        }
    }
}
