using System;
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
        ConcurrentDictionary<string, CancellationTokenSource> tokens;   //to break if free car needed
        ConcurrentQueue<CateringServiceCommand> commands;               //queue with tasks for cars
        TransportMotion.TransportMotion transportMotion;
        List<AutoResetEvent> wakeEvents = new List<AutoResetEvent>();   //to wake all cars
        ConcurrentDictionary<string, CountdownEvent> completionEvents;  //to know the command was completed
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
            tokens = new ConcurrentDictionary<string, CancellationTokenSource>();
            playDelaySource = new PlayDelaySource(timeFactor);
            transportMotion = new TransportMotion.TransportMotion(Component.Catering, mqClient,playDelaySource);            
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
                cateringCar.LocationVertex = transportMotion.GetHomeVertex();
                cars.TryAdd(cateringCar.CarId, cateringCar);
                tokens.TryAdd(cateringCar.CarId, new CancellationTokenSource());
                DoCatering(cateringCar, wakeEvents[i]).Start();
            }
        }
        void CreateQueues()
        {
            queuesFrom = new Dictionary<string, string>()
            {
                { Component.GroundMotion,Component.GroundMotion+Component.Catering },
                { Component.Airplane,Component.Airplane+Component.Catering },
                { Component.GroundService,Component.GroundService+Component.Catering },
                { Component.TimeService,Component.TimeService + Component.Catering },
            };
            queuesTo = new Dictionary<string, string>()
            {
                { Component.Airplane,Component.Catering+Component.Airplane },
                { Component.GroundService,Component.Catering+Component.GroundService },
                 { Component.Logs,Component.Logs },
                { Component.GroundMotion,Component.GroundMotion },
                { Component.Visualizer,Component.Visualizer },
            };
        }
        void DeclareQueues()
        {
            mqClient.DeclareQueues(queuesFrom.Values.ToArray());
            mqClient.DeclareQueues(queuesTo.Values.ToArray());
            
        }
        void Subscribe()
        {
            mqClient.SubscribeTo<CateringServiceCommand>(queuesFrom[Component.GroundService], cmd =>//groundservice
                    GotCommand(cmd).Start());
            mqClient.SubscribeTo<MotionPermissionResponse>(queuesFrom[Component.GroundMotion], response => //groundmotion
                    cars[response.ObjectId].MotionPermitted=true);
        }

        Task GotCommand(CateringServiceCommand cmd)     
        {
            Console.WriteLine($"Got new catering command from groundservice");
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
            foreach(var car in cars.Values)             //break cars path home
            {
                if (car.IsGoingHome)
                    tokens[car.CarId].Cancel();
            }
            foreach (var ev in wakeEvents)              //wake cars in garage
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
                while(cmd.FoodList[i].Item2>maxAmout*count)
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
            {                                                           //waits for common command
                if (commands.TryDequeue(out var command))
                {
                    Console.WriteLine($"Catering car {car.CarId} is going to airplane {command.PlaneId}");
                    transportMotion.GoPath(car, command.PlaneLocationVertex);
                    Console.WriteLine($"Catering car {car.CarId} begins catering airplane {command.PlaneId}");
                    playDelaySource.CreateToken().Sleep(1 * 60 * 1000);        //1 min to do catering
                    mqClient.Send<CateringCompletion>(queuesTo[Component.Airplane], new CateringCompletion()
                    {
                        FoodList = command.FoodList,
                        PlaneId = car.PlaneId
                    });
                    Console.WriteLine($"Catering car {car.CarId} completed catering airplane {command.PlaneId}");
                    completionEvents[car.PlaneId].Signal();
                }
                if (!IsHome(car.LocationVertex))            //if car is not home go home
                {
                    Console.WriteLine($"Catering car {car.CarId} is going home");
                    car.IsGoingHome = true;
                    transportMotion.GoPathFree(car, transportMotion.GetHomeVertex(),
                        tokens[car.CarId].Token);
                }
                if (!tokens[car.CarId].IsCancellationRequested)   //if going home was not cancelled wait for task
                {
                    car.IsGoingHome = false;
                    wakeEvent.WaitOne();                    
                }
                else
                {
                    Console.WriteLine($"Catering car {car.CarId} going home was cancelled");
                    tokens[car.CarId] = new CancellationTokenSource();
                }
             }
         }
        bool IsHome(int locationVertex)
        {
            List<int> homeVertexes = new List<int>() { 4, 10, 16, 19 };
            return homeVertexes.Contains(locationVertex);
        }
    }
    }
    

