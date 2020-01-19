﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using RabbitMqWrapper;
using AirportLibrary;
using AirportLibrary.DTO;
using AirportLibrary.Delay;
using System.Linq;
using System.Threading.Tasks;

namespace CateringComponent
{
    public class CateringComponent
    {
        Dictionary<string, string> queuesFrom;
        Dictionary<string, string> queuesTo;

        ConcurrentDictionary<string, CateringCar> cars;
        ConcurrentQueue<CateringServiceCommand> commands;
        TransportMotion.TransportMotion transportMotion;
        List<AutoResetEvent> wakeEvents = new List<AutoResetEvent>();
        ConcurrentDictionary<string, CountdownEvent> completionEvents;
        RabbitMqClient mqClient;
        Map map = new Map();
        PlayDelaySource playDelaySource;
        

        double timeFactor = 1;
        int motionInterval = 100;       //ms
        int countCars = 4;
        public CateringComponent()  
        {
            mqClient = new RabbitMqClient();
            cars = new ConcurrentDictionary<string, CateringCar>();
            commands = new ConcurrentQueue<CateringServiceCommand>();
            completionEvents = new ConcurrentDictionary<string, CountdownEvent>();
            playDelaySource = new PlayDelaySource(timeFactor);
            transportMotion = new TransportMotion.TransportMotion(Component.Catering, mqClient);            
        }
        public void Start()
        {
            CreateQueues();
            DeclareQueues();
            mqClient.PurgeQueues(queuesFrom.Values.ToArray());
            Subscribe();
            CarsStart();
        }
        void CarsStart()
        {
            for (int i = 0; i < countCars; i++)
            {
                wakeEvents.Add(new AutoResetEvent(false));
                var cateringCar = new CateringCar(i);
                cars.TryAdd(cateringCar.CarId, cateringCar);
                DoCatering(cateringCar, wakeEvents[i]).Start();
            }
        }
        void CreateQueues()
        {
            queuesFrom = new Dictionary<string, string>()
            {
                { Component.GroundMotion,Component.GroundMotion+Component.FollowMe },
                { Component.Airplane,Component.Airplane+Component.FollowMe },
                { Component.GroundService,Component.GroundService+Component.FollowMe },                
            };
            queuesTo = new Dictionary<string, string>()
            {
                { Component.Airplane,Component.FollowMe+Component.Airplane },
                { Component.GroundService,Component.FollowMe+Component.GroundService },
            };
        }
        void DeclareQueues()
        {
            mqClient.DeclareQueues(queuesFrom.Values.ToArray());
            mqClient.DeclareQueues(queuesTo.Values.ToArray());
            
        }
        void Subscribe()
        {
            mqClient.SubscribeTo<NewTimeSpeedFactor>(queuesFrom[Component.TimeService], mes =>  //timespeed
            {
                timeFactor = mes.Factor;
                playDelaySource.TimeFactor = timeFactor;
            });
            mqClient.SubscribeTo<CateringServiceCommand>(queuesFrom[Component.GroundService], cmd =>//groundservice
                    GotCommand(cmd).Start());
            mqClient.SubscribeTo<MotionPermissionResponse>(queuesFrom[Component.GroundMotion], response => //groundmotion
                    cars[response.ObjectId].MotionPermission.Set());
        }

        Task GotCommand(CateringServiceCommand cmd)     
        {
            int countCars = HowManyCarsNeeded(cmd);
            for (int i = 1; i <= countCars; i++)        //breaking the command on small commands for cars
            {
                var foodList = new List<Tuple<Food, int>>();
                commands.Enqueue(new CateringServiceCommand()
                {
                    PlaneId = cmd.PlaneId,
                    PlaneLocationVertex = cmd.PlaneLocationVertex,
                    FoodList = new List<Tuple<Food, int>>
                    (
                        cmd.FoodList.Select(tuple =>
                        {
                            var maxAmount = CateringCar.MaxFoodAmount.Find(t => t.Item1 == tuple.Item1).Item2;
                            var dif = tuple.Item2 - maxAmount * i;
                            if (dif <= 0 && dif > maxAmount * (-1))
                                return Tuple.Create(tuple.Item1, tuple.Item2);
                            else
                                return Tuple.Create(tuple.Item1, maxAmount);
                        })
                    )
                }) ;
            }
            var cde = new CountdownEvent(countCars);
            completionEvents.TryAdd(cmd.PlaneId, cde);
            foreach (var ev in wakeEvents)
                ev.Set();
            return new Task(() =>
            {
                cde.Wait();
                completionEvents.Remove(cmd.PlaneId, out cde);
                mqClient.Send<ServiceCompletionMessage>(queuesTo[Component.GroundService], new ServiceCompletionMessage()
                {
                    Component = Component.Catering,
                    PlaneId = cmd.PlaneId
                });
            });
        }
     
        int HowManyCarsNeeded(CateringServiceCommand cmd)
        {
            var cmdCat = (CateringServiceCommand)cmd;
            var count = 1;
            for(int i=0;i<cmd.FoodList.Count;i++)
            {
                var maxAmout = CateringCar.MaxFoodAmount.Find
                    (tuple => tuple.Item1 == cmdCat.FoodList[i].Item1).Item2;
                while(cmdCat.FoodList[i].Item2>maxAmout*count)
                {
                    count++;
                    break;
                }
            }
            return count;
        }
        Task DoCatering(CateringCar car, AutoResetEvent wakeEvent)      //car work
        {
            while (true)
            {
                wakeEvent.WaitOne();                        //waits for common command
                if (commands.TryDequeue(out var command))
                {                   
                    transportMotion.GoPath(car, command.PlaneLocationVertex);
                    mqClient.Send<CateringCompletion>(queuesTo[Component.Airplane], new CateringCompletion()
                    {
                        FoodList = command.FoodList,
                        PlaneId = car.PlaneId
                    });
                    completionEvents[car.PlaneId].Signal();
                    transportMotion.GoPathFree(car, transportMotion.GetHomeVertex(), wakeEvent);
                }
                wakeEvent.Reset();                      //turn to wait again
            }
        }
    }
}
