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

namespace FuelTruck
{
    public class FuelTruck
    {
        
        Dictionary<string, string> queuesFrom;
        Dictionary<string, string> queuesTo;

        ConcurrentDictionary<string, FuelTruckCar> cars;
        ConcurrentDictionary<string, CancellationTokenSource> tokens;   //to break if free car needed
        ConcurrentQueue<RefuelServiceCommand> commands;               //queue with tasks for cars
        TransportMotion.TransportMotion transportMotion;
        List<AutoResetEvent> wakeEvents = new List<AutoResetEvent>();   //to wake all cars
        ConcurrentDictionary<string, CountdownEvent> completionEvents;  //to know the command was completed
        RabbitMqClient mqClient;
        Map map = new Map();
        PlayDelaySource playDelaySource;


        double timeFactor = 1;
        int motionInterval = 100;       //ms
        int countCars = 4;
        public FuelTruck()
        {
            mqClient = new RabbitMqClient();
            cars = new ConcurrentDictionary<string, FuelTruckCar>();
            commands = new ConcurrentQueue<RefuelServiceCommand>();
            completionEvents = new ConcurrentDictionary<string, CountdownEvent>();
            tokens = new ConcurrentDictionary<string, CancellationTokenSource>();
            playDelaySource = new PlayDelaySource(timeFactor);
            transportMotion = new TransportMotion.TransportMotion(Component.FuelTruck, mqClient,playDelaySource);
        }
        public void Start()
        {
            CreateQueues();
            DeclareQueues();
            mqClient.PurgeQueues(queuesFrom.Values.ToArray());
            mqClient.PurgeQueues(queuesTo.Values.ToArray());
            Subscribe();
            CarsStart();
        }
        void CarsStart()
        {
            for (int i = 0; i < countCars; i++)
            {
                wakeEvents.Add(new AutoResetEvent(false));
                var fuelTruckCar = new FuelTruckCar(i);
                fuelTruckCar.LocationVertex = transportMotion.GetHomeVertex();
                cars.TryAdd(fuelTruckCar.CarId, fuelTruckCar);
                tokens.TryAdd(fuelTruckCar.CarId, new CancellationTokenSource());
                DoRefuel(fuelTruckCar, wakeEvents[i]).Start();
            }

            SendLogMessage(String.Format("Создали {0} машинок!", countCars));
        }
        void CreateQueues()
        {
            queuesFrom = new Dictionary<string, string>()
            {
                { Component.GroundMotion,Component.GroundMotion+Component.FuelTruck },
                { Component.Airplane,Component.Airplane+Component.FuelTruck },
                { Component.GroundService,Component.GroundService+Component.FuelTruck },
                { Component.TimeService,Component.TimeService + Component.TimeService },
            };
            queuesTo = new Dictionary<string, string>()
            {
                { Component.Airplane,Component.FuelTruck+Component.Airplane },
                { Component.GroundService,Component.FuelTruck+Component.GroundService },
                { Component.Logs, Component.Logs },
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
            mqClient.SubscribeTo<NewTimeSpeedFactor>(queuesFrom[Component.TimeService], mes =>  //timespeed
            {
                timeFactor = mes.Factor;
                playDelaySource.TimeFactor = timeFactor;
            });
            mqClient.SubscribeTo<RefuelServiceCommand>(queuesFrom[Component.GroundService], cmd =>//groundservice
                    GotCommand(cmd).Start());

            mqClient.SubscribeTo<MotionPermissionResponse>(queuesFrom[Component.GroundMotion], response => //groundmotion
                    cars[response.ObjectId].MotionPermitted = true);
        }

        //Отправка ЛОГ сообщений
        public void SendLogMessage(string message)
        {
            var newLogMessage = new LogMessage()
            {
                Message = message,
                Component = Component.FuelTruck
            };
            Console.WriteLine(message);
            mqClient.Send(queuesTo[Component.Logs], newLogMessage);
        }

        Task GotCommand(RefuelServiceCommand cmd)
        {
            Console.WriteLine($"Got new command for airplane {cmd.PlaneId}");
            int countCars = 1; // HowManyCarsNeeded(cmd); //1000 - maxFuel
            while(cmd.Fuel>0)        //breaking the command on small commands for cars
            {
                cmd.Fuel -= FuelTruckCar.MaxFuelOnBoard;
                if (cmd.Fuel > 0)
                {
                    commands.Enqueue(new RefuelServiceCommand()
                    {
                        PlaneId = cmd.PlaneId,
                        PlaneLocationVertex = cmd.PlaneLocationVertex,
                        Fuel = FuelTruckCar.MaxFuelOnBoard
                    });
                    countCars++;
                }
                else
                    commands.Enqueue(new RefuelServiceCommand()     //остаток
                    {
                        PlaneId = cmd.PlaneId,
                        PlaneLocationVertex = cmd.PlaneLocationVertex,
                        Fuel = cmd.Fuel + FuelTruckCar.MaxFuelOnBoard
                    });
            }
            var cde = new CountdownEvent(countCars);
            completionEvents.TryAdd(cmd.PlaneId, cde);
            foreach(var car in cars.Values)
            {
                if (car.IsGoingHome)
                    tokens[car.CarId].Cancel();
            }
            foreach (var ev in wakeEvents)
                ev.Set();
            return new Task(() =>
            {
                cde.Wait();
                completionEvents.Remove(cmd.PlaneId, out cde);
                mqClient.Send<ServiceCompletionMessage>(queuesTo[Component.GroundService], new ServiceCompletionMessage()
                {
                    Component = Component.FuelTruck,
                    PlaneId = cmd.PlaneId
                });
                Console.WriteLine($"Fueling of plane {cmd.PlaneId} is completed");

            });
        }

        Task DoRefuel(FuelTruckCar car, AutoResetEvent wakeEvent)      //car work
        {
            while (true)
            {                                                           //waits for common command
                if (commands.TryDequeue(out var command))
                {
                    Console.WriteLine($"Fueltruck {car.CarId} is going to fuel airplane {command.PlaneId}" +
                        $"with {command.Fuel} litres of fuel");
                    transportMotion.GoPath(car, command.PlaneLocationVertex);
                    Console.WriteLine($"Fueltruck {car.CarId} is fueling plane {command.PlaneId}");
                    playDelaySource.CreateToken().Sleep(2 * 60 * 1000);        
                    mqClient.Send<RefuelCompletion>(queuesTo[Component.Airplane], new RefuelCompletion()
                    {
                        Fuel = command.Fuel,
                        PlaneId = command.PlaneId
                    });
                    SendLogMessage(String.Format("{0} заправила самолёт {1} и поехала домой", car.CarId, command.PlaneId));
                    completionEvents[command.PlaneId].Signal();
                }
                if (!IsHome(car.LocationVertex))            //if car is not home go home
                {
                    car.IsGoingHome = true;
                    transportMotion.GoPathFree(car, transportMotion.GetHomeVertex(), tokens[car.CarId].Token);
                }                
                if (!tokens[car.CarId].IsCancellationRequested)
                {                   
                    car.IsGoingHome = false;
                    wakeEvent.WaitOne();
                }
                else
                {
                    Console.WriteLine($"Fueltruck { car.CarId} going home was cancelled");
                    tokens[car.CarId] = new CancellationTokenSource();
                }
                
            }
            bool IsHome(int locationVertex)
            {
                List<int> homeVertexes = new List<int>() { 4, 10, 16, 19 };
                return homeVertexes.Contains(locationVertex);
            }
        }
    }

}