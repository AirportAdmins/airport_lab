﻿using System;
using System.Collections.Generic;
using System.Text;
using TransportMotion;

namespace BusComponent
{
    public class BusCar : ICar
    {
        static int idCount = 0;
        string busID;
        public BusCar()
        {
            busID = "Bus-" + idCount;
            idCount++;
        }

        public CarTools CarTools { get; set; }
        public string CarId => busID;

        public bool IsGoingHome { get; set; }
        public static int PassengersMaxCount { get => 20; }
        public int Passengers { get; set; }
        public string PlaneId { get; set; }
        public bool MotionPermitted { get; set; }
        public int Speed { get => 15; set { } }
        public int LocationVertex { get; set; }
        public List<string> PassengersList { get; set; } = new List<string>();
    }
}
