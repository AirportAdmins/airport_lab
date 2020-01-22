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
            transportMotion = new TransportMotion.TransportMotion(Component.FuelTruck, mqClient);
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
                var fuelTruckCar = new FuelTruckCar(i);
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
            };
            queuesTo = new Dictionary<string, string>()
            {
                { Component.Airplane,Component.FuelTruck+Component.Airplane },
                { Component.GroundService,Component.FuelTruck+Component.GroundService },
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
                    cars[response.ObjectId].MotionPermission = true);
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
            mqClient.Send(queueToLogs, newLogMessage);
        }

        Task GotCommand(RefuelServiceCommand cmd)
        {
            int countCars = (int)Math.Celling(1000 / cmd.Fuel); // HowManyCarsNeeded(cmd); //1000 - maxFuel
            string logMes = "";
            for (int i = 1; i <= countCars; i++)        //breaking the command on small commands for cars
            {                
                commands.Enqueue(new RefuelServiceCommand()
                {
                    PlaneId = cmd.PlaneId,
                    PlaneLocationVertex = cmd.PlaneLocationVertex,
                    Fuel = cmd.Fuel
                });
                
            }
            var cde = new CountdownEvent(countCars);
            completionEvents.TryAdd(cmd.PlaneId, cde);
            foreach (var ev in wakeEvents)
                ev.Set();
            return new Task(() =>
            {
                cde.Wait();
                completionEvents.Remove(cmd.PlaneId, out cde);
                mqClient.Send<RefuelServiceCommand>(queuesTo[Component.GroundService], new ServiceCompletionMessage()
                {
                    Component = Component.FuelTruck,
                    PlaneId = cmd.PlaneId
                });

                //сюда лог мессадж

            });
        }

        /*int HowManyCarsNeeded(RefuelServiceCommand cmd)
        {
            //int cmdCat = cmd.Fuel;
            
            return (int)Math.Celling(1000/cmd.Fuel);
        }*/

        Task DoRefuel(FuelTruckCar car, AutoResetEvent wakeEvent)      //car work
        {
            while (true)
            {                                                           //waits for common command
                if (commands.TryDequeue(out var command))
                {
                    transportMotion.GoPath(car, command.PlaneLocationVertex);
                    playDelaySource.CreateToken().Sleep(10 * 60 * 1000);        //10 min to do catering
                    mqClient.Send<RefuelCompletion>(queuesTo[Component.Airplane], new FuelCompletion()
                    {
                        Fuel = command.Fuel,
                        PlaneId = car.PlaneId
                    });

                    SendLogMessage(String.Format("{0} заправила самолёт {1} и поехала домой", car.CarId, car.PlaneId));

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