using System;
using System.Collections.Generic;
using System.Text;

namespace AirplaneComponent.AirplaneGenerator
{
    public abstract class Airplane:IAirplane
    {
        protected int numberOfPassengers;
        protected int baggageAmount;
        protected int fuelAmout;
        protected bool isDeiced;
        protected int amountOfFood;
        protected string airplaneID;
        protected int flightID;

        public string AirplaneID { get => airplaneID; }
        public int FlightID { get => flightID; set => flightID = value; }
        public int NumberOfPassengers { get => numberOfPassengers; set => numberOfPassengers = value; }
        public int BaggageAmount { get => baggageAmount; set => baggageAmount = value; }
        public int FuelAmout { get => fuelAmout; set => fuelAmout = value; }
        public bool IsDeiced { get => isDeiced; set => isDeiced = value; }
        public int AmountOfFood { get => amountOfFood; set => amountOfFood = value; }

        public virtual string Name { get; }

        public virtual int PassengerCapacity { get; }

        public virtual int BaggageCapacity { get; }

        public virtual int MaxFuelAmount { get; }

    }
}
