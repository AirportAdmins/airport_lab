using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using RabbitMqWrapper;
using AirplaneComponent.AirplaneGenerator;
using AirportLibrary.DTO;
using AirportLibrary;
using System.Linq;
using System.Collections.Concurrent;
using AirportLibrary.Delay;

namespace AirplaneComponent
{
    public class AirplaneComponent
    {
        RabbitMqClient mqClient;
        ConcurrentDictionary<string, Airplane> airplanes;
        Dictionary<string, string> queuesTo;
        Dictionary<string, string> queuesFrom;
        Map map = new Map();
        PlayDelaySource source;
        double timeFactor = 1;
        Random rand = new Random();

        public AirplaneComponent()
        {
            airplanes = new ConcurrentDictionary<string, Airplane>();
            source = new PlayDelaySource(timeFactor);
        }

        public void Start()
        {
            mqClient = new RabbitMqClient();
            CreateQueues();
            DeclareQueues();
            mqClient.PurgeQueues(queuesFrom.Values.ToArray());
            mqClient.PurgeQueues(queuesTo.Values.ToArray());
            Subscribe();


        }

        void CreateQueues()
        {
            queuesTo = new Dictionary<string, string>()
            {
                { Component.Schedule, Component.Airplane + Component.Schedule },
                { Component.Bus, Component.Airplane + Component.Bus },
                { Component.Baggage, Component.Airplane + Component.Baggage },
                { Component.FollowMe, Component.Airplane + Component.FollowMe },
                { Component.Logs, Component.Airplane + Component.Logs },
                { Component.Visualizer, Component.Visualizer},
                { Component.GroundMotion, Component.GroundMotion },
                { Component.GroundService, Component.Airplane+Component.GroundService }
            };
            queuesFrom = new Dictionary<string, string>()
            {
                { Component.GroundService, Component.GroundService + Component.Airplane },
                { Component.FollowMe, Component.FollowMe + Component.Airplane },
                { Component.Schedule, Component.Schedule + Component.Airplane },
                { Component.FuelTruck, Component.FuelTruck + Component.Airplane },
                { Component.Catering, Component.Catering + Component.Airplane },
                { Component.Deicing, Component.Deicing + Component.Airplane },
                { Component.TimeService, Component.TimeService + Component.Airplane },
                { Component.GroundMotion,Component.GroundMotion+Component.Airplane },
                { Component.Bus, Component.Bus + Component.Airplane },
                { Component.Baggage, Component.Baggage + Component.Airplane },
            };
        }

        void DeclareQueues()
        {
            mqClient.DeclareQueues(queuesTo.Values.ToArray());
            mqClient.DeclareQueues(queuesFrom.Values.ToArray());
        }
        void Subscribe()
        {
            mqClient.SubscribeTo<AirplaneGenerationRequest>(queuesFrom[Component.Schedule], mes =>   //schedule
                Task.Run(() => {
                    ScheduleResponse(mes);
                    }));
            mqClient.SubscribeTo<PassengerTransferRequest>(queuesFrom[Component.Bus], mes =>    //bus
                     BusTransferResponse(mes));
            mqClient.SubscribeTo<BaggageTransferRequest>(queuesFrom[Component.Baggage], mes =>  //baggage
                     BaggageTransferResponse(mes));
            mqClient.SubscribeTo<NewTimeSpeedFactor>(queuesFrom[Component.TimeService], mes =>  //time speed
                {
                    timeFactor = mes.Factor;
                    source.TimeFactor = timeFactor;
                });
            mqClient.SubscribeTo<FollowMeCommand>(queuesFrom[Component.FollowMe], mes =>  //follow me
                     FollowAction(mes));
            mqClient.SubscribeTo<DeicingCompletion>(queuesFrom[Component.Deicing], mes =>   //deicing
            {
                lock (airplanes[mes.PlaneId])
                {
                    airplanes[mes.PlaneId].IsDeiced = true;
                }
            });
            mqClient.SubscribeTo<RefuelCompletion>(queuesFrom[Component.FuelTruck], mes =>  //fueltruck
            {
                lock (airplanes[mes.PlaneId])
                {
                    airplanes[mes.PlaneId].FuelAmount += mes.Fuel;
                }
            });
            mqClient.SubscribeTo<CateringCompletion>(queuesFrom[Component.Catering], mes => //catering
            {
                lock (airplanes[mes.PlaneId])
                {
                    var foodList = airplanes[mes.PlaneId].FoodList;
                    foreach (var foodInput in mes.FoodList)
                    {
                        foodList[foodInput.Item1] += foodInput.Item2;
                    }
                }
            });
            mqClient.SubscribeTo<DepartureSignal>(queuesFrom[Component.GroundService], mes =>   //groundservice
                     Departure(mes));
            mqClient.SubscribeTo<MotionPermissionResponse>(queuesFrom[Component.GroundMotion], mes =>//groundmotion
            {
                Console.WriteLine($"Airplane {mes.ObjectId} gets permission...");
                lock (airplanes[mes.ObjectId])
                {
                    Console.WriteLine($"Airplane {mes.ObjectId} enters lock section...");
                    airplanes[mes.ObjectId].MotionPermission.Set();
                }
            });
        }
        ///<summary>
        ///Responses
        ///<summary
        void ScheduleResponse(AirplaneGenerationRequest req)
        {
            Console.WriteLine("Request for generating a new airplane");
            Airplane airplane = Generator.Generate(req.AirplaneModelName, req.FlightId);
            if (airplanes.TryAdd(airplane.PlaneID, airplane))
            {
                Console.WriteLine("Sending permission request to GroundMotion");
                mqClient.Send(queuesTo[Component.Schedule],
                    new AirplaneGenerationResponse()
                    {
                        FlightId = req.FlightId,
                        PlaneId = airplane.PlaneID
                    });
                airplane.LocationVertex = GetVertexToLand();
                Land(airplane);
                Console.WriteLine($"Airplane {airplane.PlaneID} landed in vertex " + airplane.LocationVertex);
                AirplaneServiceCommand(airplane);
            }
        }
        void BusTransferResponse(PassengerTransferRequest req)
        {
            Airplane plane = airplanes[req.PlaneId];
            lock (plane)
            {
                if (req.Action == TransferAction.Take)
                {
                    mqClient.Send(queuesTo[Component.Bus], new PassengersTransfer()
                    {
                        BusId = req.BusId,
                        PassengerCount = req.PassengersCount
                    });
                    plane.Passengers -= req.PassengersCount;
                }
                else
                {
                    plane.Passengers += req.PassengersCount;
                }
            }
        }
        void BaggageTransferResponse(BaggageTransferRequest req)
        {
            Airplane plane = airplanes[req.PlaneId];
            lock (plane)
            {
                if (req.Action == TransferAction.Take)
                {
                    mqClient.Send<BaggageTransfer>(queuesTo[Component.Baggage], new BaggageTransfer()
                    {
                        BaggageCarId = req.BaggageCarId,
                        BaggageCount = req.BaggageCount
                    });
                    plane.BaggageAmount -= req.BaggageCount;
                }
                else
                {
                    plane.BaggageAmount += req.BaggageCount;
                }
            }
        }
        void AirplaneServiceCommand(Airplane plane)
        {
            mqClient.Send(queuesTo[Component.GroundService],
                new AirplaneServiceCommand()
                {
                    LocationVertex = plane.LocationVertex,
                    PlaneId = plane.PlaneID,
                    FlightId = plane.FlightID,
                    Needs = new List<Tuple<AirplaneNeeds, int>>()
                    {
                        Tuple.Create(AirplaneNeeds.PickUpPassengers,plane.Passengers),
                        Tuple.Create(AirplaneNeeds.PickUpBaggage,plane.BaggageAmount),
                        Tuple.Create(AirplaneNeeds.Refuel,plane.Model.Fuel-plane.FuelAmount)
                    }
                });
        }
        void FollowAction(FollowMeCommand cmd)
        {
            var plane = airplanes[cmd.PlaneId];
            int distance = GetDistance(plane.LocationVertex, cmd.DestinationVertex);
            SendVisualizationMessage(plane, cmd.DestinationVertex, Airplane.SpeedOnGround);           
            Console.WriteLine("Go to vertex " + cmd.DestinationVertex + " with followme");
            Task task = Task.Run(() =>
            {
                source.CreateToken().Sleep(distance * 1000 / Airplane.SpeedOnGround);
                SendVisualizationMessage(plane, cmd.DestinationVertex, 0);
                plane.LocationVertex = cmd.DestinationVertex;
                mqClient.Send(queuesTo[Component.FollowMe], new ArrivalConfirmation()
                {
                    PlaneId = plane.PlaneID,
                    FollowMeId = cmd.FollowMeId,
                    LocationVertex = plane.LocationVertex
                });
                Console.WriteLine("In vertex " + plane.LocationVertex);
            });
        }
        void SendVisualizationMessage(Airplane plane, int DestinationVertex, int speed)
        {
            mqClient.Send<VisualizationMessage>(queuesTo[Component.Visualizer], new VisualizationMessage()
            {
                StartVertex = plane.LocationVertex,
                DestinationVertex = DestinationVertex,
                Speed = speed,
                ObjectId = plane.PlaneID,
                Type = plane.Model.Model
            });
        }

        void Departure(DepartureSignal signal)
        {
            var plane=airplanes[signal.PlaneId];
            MoveByItself(plane, plane.LocationVertex - 4);
        }

   
        void Land(Airplane plane)
        {
            MoveByItself(plane, plane.LocationVertex + 4).Wait();
        }

        Task MoveByItself(Airplane plane, int DestinationVertex)
        {
            int distance = GetDistance(plane.LocationVertex, DestinationVertex);
            WaitForMotionPermission(plane,DestinationVertex);

            Console.WriteLine("Go to vertex "+DestinationVertex+" alone");
            SendVisualizationMessage(plane, DestinationVertex, Airplane.SpeedFly);
            Console.WriteLine("Send vs message");
            Task task = new Task(() =>
            {
                source.CreateToken().Sleep(distance * 1000 / Airplane.SpeedFly);
                SendVisualizationMessage(plane, DestinationVertex, 0);

                Console.WriteLine("Send vs message");
                mqClient.Send<MotionPermissionRequest>(queuesTo[Component.GroundMotion], new MotionPermissionRequest()

                {
                    Action = MotionAction.Free,
                    Component = Component.Airplane,
                    DestinationVertex = DestinationVertex,
                    ObjectId = plane.PlaneID,
                    StartVertex = plane.LocationVertex
                });
                plane.LocationVertex = DestinationVertex;
                plane.MotionPermitted = false;
                Console.WriteLine($"Airlane {plane.PlaneID} is now in vertex " + DestinationVertex);

            });
            task.Start();
            return task;
        }
        void WaitForMotionPermission(Airplane airplane, int DestinationVertex)
        {
            mqClient.Send(queuesTo[Component.GroundMotion],
                new MotionPermissionRequest()
                {
                    Action = MotionAction.Occupy,
                    Component = Component.Airplane,
                    DestinationVertex = DestinationVertex,
                    ObjectId = airplane.PlaneID,
                    StartVertex = airplane.LocationVertex
                });
            Console.WriteLine($"Airplane {airplane.PlaneID} starts waiting for permission...");
            airplane.MotionPermission.WaitOne();
        }

        int GetDistance(int locationVertex, int destinationVertex)
        {
            return map.Graph.GetWeightBetweenNearVerties(locationVertex, destinationVertex);
        }
        int GetVertexToLand()
        {

            lock (rand)
            {
                List<int> vertexes = new List<int>() { 1, 2, 3 };
                return vertexes[rand.Next(0, 3)];
            }
        }
        }
    }

    


