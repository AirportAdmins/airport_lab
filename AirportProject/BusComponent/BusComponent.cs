using AirportLibrary;
using AirportLibrary.Delay;
using AirportLibrary.DTO;
using RabbitMqWrapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BusComponent
{
    public class BusComponent
    {
        Dictionary<string, string> queuesFrom;
        Dictionary<string, string> queuesTo;

        ConcurrentDictionary<string, BusCar> cars;
        ConcurrentDictionary<string, CancellationTokenSource> tokens;   //to break if free car needed
        ConcurrentQueue<PassengersServiceCommand> commands;               //queue with tasks for cars
        TransportMotion.TransportMotion transportMotion;
        List<AutoResetEvent> wakeEvents = new List<AutoResetEvent>();   //to wake all cars
        ConcurrentDictionary<string, CountdownEvent> completionEvents;  //to know the command was completed
        RabbitMqClient mqClient;
        Map map = new Map();
        PlayDelaySource playDelaySource;


        double timeFactor = 1;
        int motionInterval = 100;       //ms
        int countCars = 4;
        public BusComponent()
        {
            mqClient = new RabbitMqClient();
            cars = new ConcurrentDictionary<string, BusCar>();
            commands = new ConcurrentQueue<PassengersServiceCommand>();
            completionEvents = new ConcurrentDictionary<string, CountdownEvent>();
            tokens = new ConcurrentDictionary<string, CancellationTokenSource>();
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
                var busCar = new BusCar();
                cars.TryAdd(busCar.CarId, busCar);
                tokens.TryAdd(busCar.CarId, new CancellationTokenSource());
                DoWork(busCar, wakeEvents[i]).Start();
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
            mqClient.SubscribeTo<PassengersServiceCommand>(queuesFrom[Component.GroundService], cmd =>//groundservice
                    GotCommand(cmd).Start());
            mqClient.SubscribeTo<MotionPermissionResponse>(queuesFrom[Component.GroundMotion], response => //groundmotion
                    cars[response.ObjectId].MotionPermitted = true);

        }

        Task GotCommand(CateringServiceCommand cmd)
        {
            DoSmallCommmands(cmd);                      //breaking a command on small commands
          
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

        void DoSmallCommands(PassengersServiceCommand cmd)
        {
            var count = 1;
            while(cmd.PassengersCount>0)
            {
                cmd.PassengersCount -=  BusCar.PassengersMaxCount * count; //TODO WTF
                if (cmd.PassengersCount>=0)
                {
                    commands.Enqueue(new PassengersServiceCommand()
                    {
                        Action=cmd.Action,
                        FlightId=cmd.FlightId,
                        PassengersCount=BusCar.PassengersMaxCount,
                        PlaneId=cmd.PlaneId,
                        PlaneLocationVertex=cmd.PlaneLocationVertex
                    });
                }
                else
                {
                    commands.Enqueue(new PassengersServiceCommand()
                    {
                        Action = cmd.Action,
                        FlightId = cmd.FlightId,
                        PassengersCount = cmd.PassengersCount+BusCar.PassengersMaxCount,
                        PlaneId = cmd.PlaneId,
                        PlaneLocationVertex = cmd.PlaneLocationVertex
                    });
                }
                count++;
            }
        }
        Task DoWork(BusCar car, AutoResetEvent wakeEvent)      //car work
        {
            while (true)
            {                                                           //waits for common command
                if (commands.TryDequeue(out var command))
                {
                    transportMotion.GoPath(car, command.PlaneLocationVertex);
                    playDelaySource.CreateToken().Sleep(10 * 60 * 1000);        //10 min to do catering
                    mqClient.Send<CateringCompletion>(queuesTo[Component.Airplane], new CateringCompletion()
                    {
                        FoodList = command.FoodList,
                        PlaneId = car.PlaneId
                    });
                    completionEvents[car.PlaneId].Signal();
                    transportMotion.GoPathFree(car, transportMotion.GetHomeVertex(), tokens[car.CarId].Token);
                    if (!tokens[car.CarId].IsCancellationRequested)
                        wakeEvent.WaitOne();
                    else
                    {
                        tokens[car.CarId] = new CancellationTokenSource();
                    }
                }
            }
        }
    }
}
