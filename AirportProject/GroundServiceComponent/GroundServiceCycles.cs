﻿using System;
using System.Linq;
using AirportLibrary;
using RabbitMqWrapper;
using AirportLibrary.DTO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GroundServiceComponent
{
    public enum ActionStatus
    {
        NotStarted, Started, Finished
    }
    public class Cycle
    {
        public readonly object lockStatus = new object();
        static int count;
        public int Id { get; }
        public ILogger logger;
        public ActionStatus Status = ActionStatus.NotStarted;
        public Dictionary<string, ActionStatus> componentsAction { get; }
        
        public Cycle(ILogger logger,params string[] actions)
        {
            this.logger = logger;
            Id = count++;
            componentsAction = new Dictionary<string, ActionStatus>();
            foreach (var component in actions)
                componentsAction[component] = ActionStatus.NotStarted;
        }
        public void FinishAction(string component)
        {
            lock (lockStatus)
            {
                if (componentsAction[component] == ActionStatus.Finished)
                {
                    //logger
                    throw new Exception();
                }
                componentsAction[component] = ActionStatus.Finished;

                logger?.Debug($"{GroundServiceComponent.ComponentName}: FinishAction: InnerCycle Id {Id} Action {component} is finished");
                if (componentsAction.Keys.Where(x => componentsAction[x] == ActionStatus.Finished).Count() == componentsAction.Count)
                {
                    this.Status = ActionStatus.Finished;
                    logger?.Debug($"{GroundServiceComponent.ComponentName}: InnerCycle Id {Id} is finished");
                }
            }
        }

    }
    public class GroundServiceCycles
    {
        static int count = 0;
        static readonly string[] FirstCycleComponents = new string[]
        {
            Component.FollowMe,
            Component.Bus,
            Component.Baggage,
            Component.Deicing,
            Component.FuelTruck
        };
        static readonly string[] SecondCycleComponents = new string[]
        {
            Component.Catering,
            Component.Bus,
            Component.Baggage,
            Component.FollowMe
        };
        static object parkingVerticesLock = new object();
        static List<int> runWayVertices = new List<int>() { 5, 6, 7 };
        static List<int> freeParkingvertices = new List<int>() { 8, 9, 11, 12, 17, 18, 23, 24 } ;

        int id;
        Cycle firstCycle;
        Cycle secondCycle;

        RabbitMqClient mqClient;
        ILogger logger;

        public string PlaneId;
        public string FlightId;
        public int PlaneLocationVertex;

        public GroundServiceCycles(RabbitMqClient mq, ILogger logger)
        {
            id = count++;
            firstCycle = new Cycle(logger, FirstCycleComponents);
            secondCycle = new Cycle(logger, SecondCycleComponents);
            this.logger = logger;
            mqClient = mq;
            logger?.Debug($"{GroundServiceComponent.ComponentName}: Create new service cycle with Id {id} ");
        }
        public async void StartFisrtCycle(object needs)
        {
            lock(firstCycle.lockStatus)
                firstCycle.Status = ActionStatus.Started;

            logger?.Info($"{GroundServiceComponent.ComponentName}: Started first cycle in cycle with Id {id} (PlaneId: {PlaneId}, FlightId: {FlightId})");

            RequestFollow(freeParkingvertices, true);

            //Wait End of Park
            while (firstCycle.componentsAction[Component.FollowMe]!= ActionStatus.Finished)
                await Task.Delay(100); //Maybe - Threading.Sleep(100)

            int passCount, baggCount, fuelCount;
            try
            {
                passCount = ((List<Tuple<AirplaneNeeds, int>>)needs).Where(x => x.Item1 == AirplaneNeeds.PickUpPassengers).First().Item2;
                baggCount = ((List<Tuple<AirplaneNeeds, int>>)needs).Where(x => x.Item1 == AirplaneNeeds.PickUpBaggage).First().Item2;
                fuelCount = ((List<Tuple<AirplaneNeeds, int>>)needs).Where(x => x.Item1 == AirplaneNeeds.Refuel).First().Item2;
            }
            catch 
            {
                //log error
                throw new Exception();
            }
            RequestMovePassengers(TransferAction.Take, passCount);
            RequestMoveBaggage(TransferAction.Take, baggCount);
            RequestDeice();

            //Wait End of UnloadPassengers
            while (firstCycle.componentsAction[Component.Bus]!= ActionStatus.Finished)
                await Task.Delay(100);
            RequestReplenishFuel(fuelCount);
        }
        public async void StartSecondCycle(object mes)
        {
            logger?.Info($"{GroundServiceComponent.ComponentName}: Try to start second cycle in cycle with Id {id} (PlaneId: {PlaneId}, FlightId: {FlightId})");

            while (firstCycle.Status != ActionStatus.Finished)
                await Task.Delay(100);

            logger?.Info($"{GroundServiceComponent.ComponentName}: Started second cycle in cycle with {id} (PlaneId: {PlaneId}, FlightId: {FlightId})");

            lock (secondCycle.lockStatus)
                secondCycle.Status = ActionStatus.Started;

            RequestMovePassengers(TransferAction.Give, ((FlightInfo)mes).PassengerCount);
            RequestMoveBaggage(TransferAction.Give, ((FlightInfo)mes).BaggageCount);
            RequestDeliverEat(((FlightInfo)mes).FoodList);

            //Wait End of overhead actions
            while (secondCycle.componentsAction[Component.Bus] != ActionStatus.Finished ||
                    secondCycle.componentsAction[Component.Baggage] != ActionStatus.Finished ||
                    secondCycle.componentsAction[Component.Catering] != ActionStatus.Finished)
                await Task.Delay(100);
            RequestFollow(runWayVertices, false);
        }
        public async void StartDeparture()
        {
            if (secondCycle.Status != ActionStatus.Finished)
            {
                mqClient.Send<AirplaneServiceStatus>(GroundServiceComponent.ComponentName + Component.Schedule,
                new AirplaneServiceStatus()
                {
                    PlaneId = this.PlaneId,
                    Status = ServiceStatus.Delayed
                });
                while (secondCycle.Status != ActionStatus.Finished)
                    await Task.Delay(100);
            }
            mqClient.Send<DepartureSignal>(GroundServiceComponent.ComponentName + Component.Airplane,
                new DepartureSignal()
                {
                    PlaneId = this.PlaneId
                });
            Console.WriteLine($"Sent DepartureSignal to {PlaneId}");
            mqClient.Send<AirplaneServiceStatus>(GroundServiceComponent.ComponentName + Component.Schedule,
                new AirplaneServiceStatus()
                {
                    PlaneId = this.PlaneId,
                    Status = ServiceStatus.Departed
                });
        }
        public void FinishAction(object component)
        {
            if (firstCycle.Status != ActionStatus.Finished)
                firstCycle.FinishAction((string)component);
            else
                secondCycle.FinishAction((string)component);
        }
        void RequestFollow(List<int> verticesSet, bool isParkingAction)
        {
            lock (parkingVerticesLock)
            {
                var newVertex = verticesSet[new Random().Next(0, verticesSet.Count)];
                mqClient.Send<AirplaneTransferCommand>(GroundServiceComponent.ComponentName + Component.FollowMe,
                    new AirplaneTransferCommand()
                    {
                        PlaneLocationVertex = this.PlaneLocationVertex,
                        DestinationVertex = newVertex,
                        PlaneId = this.PlaneId
                    });
                if (isParkingAction)
                    freeParkingvertices.Remove(newVertex);
                else
                    freeParkingvertices.Add(PlaneLocationVertex);
                PlaneLocationVertex = newVertex;
            }
            logger?.Info($"{GroundServiceComponent.ComponentName}: Send request to FollowMe (PlaneId: {PlaneId}, FlightId: {FlightId})");
        }
        void RequestMovePassengers(TransferAction action, int passengerCount)
        {
            mqClient.Send<PassengersServiceCommand>(GroundServiceComponent.ComponentName + Component.Bus,
                new PassengersServiceCommand()
                {
                    PlaneLocationVertex = this.PlaneLocationVertex,
                    PlaneId = this.PlaneId,
                    FlightId = this.FlightId,
                    Action = action,
                    PassengersCount = passengerCount
                });
            logger?.Info($"{GroundServiceComponent.ComponentName}: Send request to Bus (PlaneId: {PlaneId}, FlightId: {FlightId}, Passengers Count: {passengerCount})");
        }
        void RequestMoveBaggage(TransferAction action, int baggageCount)
        {
            mqClient.Send<BaggageServiceCommand>(GroundServiceComponent.ComponentName + Component.Baggage,
                new BaggageServiceCommand()
                {
                    PlaneLocationVertex = this.PlaneLocationVertex,
                    PlaneId = this.PlaneId,
                    FlightId = this.FlightId,
                    Action = action,
                    BaggageCount = baggageCount
                });
            logger?.Info($"{GroundServiceComponent.ComponentName}: Send request to Baggage (PlaneId: {PlaneId}, FlightId: {FlightId}, BaggageCount: {baggageCount})");
        }
        void RequestDeice()
        {
            mqClient.Send<ServiceCommand>(GroundServiceComponent.ComponentName + Component.Deicing,
                new ServiceCommand()
                {
                    PlaneLocationVertex = this.PlaneLocationVertex,
                    PlaneId = this.PlaneId,
                });
            logger?.Info($"{GroundServiceComponent.ComponentName}: Send request to Deicing (PlaneId: {PlaneId}, FlightId: {FlightId})");
        }
        void RequestReplenishFuel(int fuelCount)
        {
            mqClient.Send<RefuelServiceCommand>(GroundServiceComponent.ComponentName + Component.FuelTruck,
                new RefuelServiceCommand()
                {
                    PlaneLocationVertex = this.PlaneLocationVertex,
                    PlaneId = this.PlaneId,
                    Fuel = fuelCount
                });
            logger?.Info($"{GroundServiceComponent.ComponentName}: Send request to Fueltruck (PlaneId: {PlaneId}, FlightId: {FlightId}, FuelCount: {fuelCount})");
        }
        void RequestDeliverEat(List<Tuple<Food, int>> foodList)
        {
            mqClient.Send<CateringServiceCommand>(GroundServiceComponent.ComponentName + Component.Catering,
                new CateringServiceCommand()
                {
                    PlaneLocationVertex = this.PlaneLocationVertex,
                    PlaneId = this.PlaneId,
                    FoodList = foodList
                });
            logger?.Info($"{GroundServiceComponent.ComponentName}: Send request to Catering (PlaneId: {PlaneId}, FlightId: {FlightId})");
        }
    }
}
