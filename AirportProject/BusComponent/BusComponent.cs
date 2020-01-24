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
    public class CarTools
    {
        public AutoResetEvent WakeEvent { get; set; }
        public AutoResetEvent AirplaneResponse { get; set; }
        public AutoResetEvent StorageResponse { get; set; }
        public CancellationTokenSource TokenSource { get; set; }
    }
    public class BusComponent
    {
        Dictionary<string, string> queuesFrom;
        Dictionary<string, string> queuesTo;

        ConcurrentDictionary<string, BusCar> cars;
        ConcurrentQueue<PassengersServiceCommand> commands;             //queue with commands for cars
        TransportMotion.TransportMotion transportMotion;
        ConcurrentDictionary<string, CountdownEvent> completionEvents;  //to know the big command was completed
        RabbitMqClient mqClient;
        Map map = new Map();
        PlayDelaySource playDelaySource;
        int storageVertex = 25;

        double timeFactor = 1;
        int motionInterval = 100;       //ms
        int countCars = 4;
        public BusComponent()
        {
            mqClient = new RabbitMqClient();
            cars = new ConcurrentDictionary<string, BusCar>();
            commands = new ConcurrentQueue<PassengersServiceCommand>();
            completionEvents = new ConcurrentDictionary<string, CountdownEvent>();
            playDelaySource = new PlayDelaySource(timeFactor);
            transportMotion = new TransportMotion.TransportMotion(Component.Bus, mqClient, playDelaySource);
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
                var busCar = new BusCar();
                busCar.LocationVertex = transportMotion.GetHomeVertex();
                cars.TryAdd(busCar.CarId, busCar);
                busCar.CarTools = new CarTools()
                {
                    AirplaneResponse = new AutoResetEvent(false),
                    WakeEvent = new AutoResetEvent(false),
                    StorageResponse = new AutoResetEvent(false),
                    TokenSource = new CancellationTokenSource()
                };
                DoWork(busCar, cars[busCar.CarId].CarTools.WakeEvent).Start();
            }
        }
        void CreateQueues()
        {
            queuesFrom = new Dictionary<string, string>()
            {
                { Component.GroundMotion,Component.GroundMotion+Component.Bus },
                { Component.Airplane,Component.Airplane+Component.Bus },
                { Component.GroundService,Component.GroundService+Component.Bus },
                { Component.Storage,Component.Storage+Component.Bus },
                { Component.TimeService,Component.TimeService + Component.Bus },
            };
            queuesTo = new Dictionary<string, string>()
            {
                { Component.Airplane,Component.Bus+Component.Airplane },
                { Component.GroundService,Component.Bus+Component.GroundService },
                { Component.Storage,Component.Bus+Component.Storage },
                { Component.Passenger,Component.Bus+Component.Passenger },
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

            mqClient.SubscribeTo<PassengersServiceCommand>(queuesFrom[Component.GroundService], cmd =>//groundservice
                    GotCommand(cmd).Start());
            mqClient.SubscribeTo<MotionPermissionResponse>(queuesFrom[Component.GroundMotion], response => //groundmotion
                    cars[response.ObjectId].MotionPermitted = true);
            mqClient.SubscribeTo<PassengersTransfer>(queuesFrom[Component.Airplane], tr =>
            {
                var car = cars[tr.BusId];
                car.Passengers = tr.PassengerCount;
                Console.WriteLine($"Bus {car.CarId} took {tr.PassengerCount} from airplane");
                car.CarTools.AirplaneResponse.Set();
            });
            mqClient.SubscribeTo<PassengersFromStorageResponse>(queuesFrom[Component.Storage], resp =>
                    StorageResponse(resp));
        }

        void StorageResponse(PassengersFromStorageResponse resp)
        {
            mqClient.Send<PassengerPassMessage>(queuesTo[Component.Passenger], new PassengerPassMessage()
            {
                ObjectId = resp.BusId,
                PassengersIds = resp.PassengersIds,
                Status = PassengerStatus.InBus
            });
            cars[resp.BusId].Passengers = resp.PassengersCount;
            cars[resp.BusId].CarTools.StorageResponse.Set();
            Console.WriteLine($"Bus {cars[resp.BusId].CarId} took {resp.PassengersCount} passengers from storage");
        }
        Task GotCommand(PassengersServiceCommand cmd)
        {
            Console.WriteLine($"Got a new command of {cmd.Action} passengers for an airplane {cmd.PlaneId}" +
                $"passengercount= {cmd.PassengersCount}");
            var count = DoSmallCommands(cmd);                      //breaking a command on small commands  
            Console.WriteLine($"Broke a command for {cmd.PlaneId} on {count} commands");
            var cde = new CountdownEvent(count);
            completionEvents.TryAdd(cmd.PlaneId, cde);
            foreach (var car in cars.Values)        //cancel going home
            {
                if (car.IsGoingHome)
                    car.CarTools.TokenSource.Cancel();
            }
            foreach (var car in cars.Values)        //wake up the cars in garage
                car.CarTools.WakeEvent.Set();
            return new Task(() =>
            {
                cde.Wait();
                Console.WriteLine($"Completion servicing airplane {cmd.PlaneId} ");
                completionEvents.Remove(cmd.PlaneId, out cde);
                mqClient.Send<ServiceCompletionMessage>(queuesTo[Component.GroundService], new ServiceCompletionMessage()
                {
                    Component = Component.Bus,
                    PlaneId = cmd.PlaneId
                });
            });
        }

        int DoSmallCommands(PassengersServiceCommand cmd)
        {
            var count = 0;
            while(cmd.PassengersCount>0)
            {
                count++;
                cmd.PassengersCount -=  BusCar.PassengersMaxCount; //how many passengers left
                if (cmd.PassengersCount>0)
                {
                    commands.Enqueue(new PassengersServiceCommand()
                    {
                        Action = cmd.Action,
                        FlightId = cmd.FlightId,
                        PassengersCount = BusCar.PassengersMaxCount,
                        PlaneId = cmd.PlaneId,
                        PlaneLocationVertex = cmd.PlaneLocationVertex
                    });
                    
                }
                else
                {
                    commands.Enqueue(new PassengersServiceCommand()
                    {
                        Action = cmd.Action,
                        FlightId = cmd.FlightId,
                        PassengersCount = cmd.PassengersCount + BusCar.PassengersMaxCount,
                        PlaneId = cmd.PlaneId,
                        PlaneLocationVertex = cmd.PlaneLocationVertex
                    });
                }
            }
            return count;
        }
        Task DoWork(BusCar car, AutoResetEvent wakeEvent)         //car work
        {
            return new Task(() =>
            {
                while (true)
                {
                    Console.WriteLine($"Bus {car.CarId} is trying to get command");
                    if (commands.TryDequeue(out var command))
                    {
                        if (command.Action == TransferAction.Give)
                            GetPassengersToAirplane(car, command);
                        else
                            TakePassengersFromAirplane(car, command);
                        completionEvents[command.PlaneId].Signal();
                    }
                    if (!IsHome(car.LocationVertex))            //if car is not home go home
                    {
                        Console.WriteLine($"Bus {car.CarId} is going home");
                        car.IsGoingHome = true;
                        transportMotion.GoPathFree(car, transportMotion.GetHomeVertex(),
                            car.CarTools.TokenSource.Token);
                    }
                    if (!car.CarTools.TokenSource.IsCancellationRequested)   //if going home was not cancelled wait for task
                    {
                        car.IsGoingHome = false;
                        wakeEvent.WaitOne();
                    }
                    else
                    {
                        car.CarTools.TokenSource = new CancellationTokenSource();
                    }
                }
            });
        }

        bool IsHome(int locationVertex)
        {
            List<int> homeVertexes = new List<int>() { 4, 10, 16, 19 };
            return homeVertexes.Contains(locationVertex);
        }
        void TakePassengersFromAirplane(BusCar car, PassengersServiceCommand cmd)
        {
            Console.WriteLine($"Bus {car.CarId} is going to take passngers from airplane {cmd.PlaneId}");
            transportMotion.GoPath(car, cmd.PlaneLocationVertex);
            Console.WriteLine($"Bus {car.CarId} begins to take passengers from airplane {cmd.PlaneId} ");
            playDelaySource.CreateToken().Sleep(2 * 60 * 1000);      //take passengers from airplane
            mqClient.Send<PassengerTransferRequest>(queuesTo[Component.Airplane], new PassengerTransferRequest()
            {
                Action = TransferAction.Take,
                BusId = car.CarId,
                PassengersCount = BusCar.PassengersMaxCount,
                PlaneId = cmd.PlaneId
            });
            car.CarTools.AirplaneResponse.WaitOne();
            Console.WriteLine($"Bus {car.CarId} has taken passengers from airplane {cmd.PlaneId} " +
                $"and going to storage");
            transportMotion.GoPath(car, 25);
            Console.WriteLine($"Bus {car.CarId} begin to transfer passengers to storage ");
            playDelaySource.CreateToken().Sleep(2 * 60 * 1000);  //just throw passengers in the rain
            Console.WriteLine($"Bus {car.CarId} has transfered passengers to storage");
            car.Passengers = 0;
        }
        void GetPassengersToAirplane(BusCar car, PassengersServiceCommand cmd)
        {
            Console.WriteLine($"Bus {car.CarId} is going to get passengers to airplane {cmd.PlaneId}");
            Console.WriteLine($"Bus {car.CarId} is going to storage");
            transportMotion.GoPath(car, 25);

            mqClient.Send<PassengersFromStorageRequest>(queuesTo[Component.Storage], new PassengersFromStorageRequest()
            {
                BusId = car.CarId,
                Capacity = BusCar.PassengersMaxCount,
                FlightId = cmd.FlightId
            });
            Console.WriteLine($"Bus {car.CarId} send message to storage");
            car.CarTools.StorageResponse.WaitOne();
            Console.WriteLine($"Bus {car.CarId} took passengers from storage and is going to plane {cmd.PlaneId}");
            transportMotion.GoPath(car, cmd.PlaneLocationVertex);
            Console.WriteLine($"Bus {car.CarId} begins to give passengers to airplane {cmd.PlaneId} ");
            playDelaySource.CreateToken().Sleep(2 * 60 * 1000);        //get passengers to airplane
            mqClient.Send<PassengerTransferRequest>(queuesTo[Component.Airplane], new PassengerTransferRequest()
            {
                Action = TransferAction.Give,
                BusId = car.CarId,
                PassengersCount = car.Passengers,
                PlaneId = cmd.PlaneId
            });
            Console.WriteLine($"Bus {car.CarId} gave passengers to storage");
            car.Passengers = 0;
        }

    }
}