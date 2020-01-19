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
            mqClient.PurgeQueues(queuesFrom[Component.Schedule],queuesTo[Component.Schedule]);
            Subscribe();
            //Console.WriteLine("Okay");            
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
                { Component.Visualizer, Component.Airplane + Component.Visualizer },
                {Component.GroundMotion, Component.Airplane+Component.GroundMotion }
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
                     ScheduleResponse(mes));
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
                     airplanes[mes.PlaneId].IsDeiced = true);
            mqClient.SubscribeTo<RefuelCompletion>(queuesFrom[Component.FuelTruck], mes =>  //fueltruck
                     airplanes[mes.PlaneId].FuelAmount += mes.Fuel);
            mqClient.SubscribeTo<CateringCompletion>(queuesFrom[Component.Catering], mes => //catering
                     airplanes[mes.PlaneId].FoodList = mes.FoodList);
            mqClient.SubscribeTo<DepartureSignal>(queuesFrom[Component.GroundService], mes =>   //groundservice
                     Departure(mes));
            mqClient.SubscribeTo<MotionPermissionResponse>(queuesFrom[Component.GroundService], mes =>//groundmotion
            {
                airplanes[mes.ObjectId].MotionPermitted = true;
            });
        }
        ///<summary>
        ///Responses
        ///<summary
        void ScheduleResponse(AirplaneGenerationRequest req)
        {
            Airplane airplane = Generator.Generate(req.AirplaneModelName, req.FlightId);
            if (airplanes.TryAdd(airplane.PlaneID, airplane))
            {
                mqClient.Send<AirplaneGenerationResponse>(queuesTo[Component.Schedule],
                    new AirplaneGenerationResponse()
                    {
                        FlightId = req.FlightId,
                        PlaneId = airplane.PlaneID
                    });
                airplane.LocationVertex = GetVertexToLand();
                Land(airplane);
                AirplaneServiceCommand(airplane);
            }
        }
        void BusTransferResponse(PassengerTransferRequest req)
        {
            Airplane plane = airplanes[req.PlaneId];
            if (req.Action == TransferAction.Take)
            {
                mqClient.Send<PassengersTransfer>(queuesTo[Component.Bus], new PassengersTransfer()
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
        void BaggageTransferResponse(BaggageTransferRequest req)
        {
            Airplane plane = airplanes[req.PlaneId];
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
        void AirplaneServiceCommand(Airplane plane)
        {
            mqClient.Send<AirplaneServiceCommand>(queuesTo[Component.GroundService],
                new AirplaneServiceCommand()
                {
                    LocationVertex = plane.LocationVertex,
                    PlaneId = plane.PlaneID,
                    FlightId= plane.FlightID,
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
            int timeInterval = 100;
            double position = 0;
            var plane = airplanes[cmd.PlaneId];
            int distance = GetDistance(plane.LocationVertex, cmd.DestinationVertex);
            SendVisualizationMessage(plane, cmd.DestinationVertex, Airplane.SpeedOnGround);
            Task task = Task.Run(() =>
            {
                while (position < distance)
                {
                    position += Airplane.SpeedOnGround/3.6/1000 * timeInterval * timeFactor;
                    source.CreateToken().Sleep(timeInterval);
                };
                SendVisualizationMessage(plane, cmd.DestinationVertex, 0);
                plane.LocationVertex = cmd.DestinationVertex;
                mqClient.Send<ArrivalConfirmation>(queuesTo[Component.FollowMe], new ArrivalConfirmation()
                {
                    PlaneId = plane.PlaneID,
                    FollowMeId = cmd.FollowMeId,
                    LocationVertex = plane.LocationVertex
                });                
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
            FlyAway(airplanes[signal.PlaneId]);
        }

        void FlyAway(Airplane plane)
        {
            MoveByItself(plane, plane.LocationVertex - 4);
        }
        void Land(Airplane plane)
        {
            MoveByItself(plane, plane.LocationVertex + 4);
        }

        void MoveByItself(Airplane plane, int DestinationVertex)
        {
            int timeInterval = 100;
            double position = 0;
            int distance = GetDistance(plane.LocationVertex, DestinationVertex);
            WaitForMotionPermission(plane,DestinationVertex);
            SendVisualizationMessage(plane, DestinationVertex, Airplane.SpeedFly);
            Task task = Task.Run(() =>
            {
                while (position < distance)
                {
                    position += Airplane.SpeedFly/3.6/1000 * timeInterval * timeFactor; //m/ms
                    source.CreateToken().Sleep(timeInterval);
                };
                SendVisualizationMessage(plane, DestinationVertex, 0);
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
            });
        }
        void WaitForMotionPermission(Airplane airplane, int DestinationVertex)
        {
            mqClient.Send<MotionPermissionRequest>(queuesTo[Component.GroundMotion],
                new MotionPermissionRequest()
                {
                    Action = MotionAction.Occupy,
                    Component = Component.Airplane,
                    DestinationVertex = DestinationVertex,
                    ObjectId = airplane.PlaneID,
                    StartVertex = airplane.LocationVertex
                });

            while (!airplane.MotionPermitted)
                source.CreateToken().Sleep(5);
        }

        int GetDistance(int locationVertex, int destinationVertex)   
        {
            return map.Graph.GetWeightBetweenNearVerties(locationVertex, destinationVertex);
        }
        int GetVertexToLand()
        {
            Random rand = new Random();
            List<int> vertexes = new List<int>() { 1, 2, 3 };
            return vertexes[rand.Next(0, 2)];
        }
        
    }
}

