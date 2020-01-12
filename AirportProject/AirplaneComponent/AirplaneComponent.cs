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

namespace AirplaneComponent
{
    public class AirplaneComponent
    {
        RabbitMqClient MqClient;
        ConcurrentDictionary<string, Airplane> airplanes;
        Dictionary<string, string> queuesTo;
        Dictionary<string, string> queuesFrom;
        double TimeSpeedFactor = 1;
        public AirplaneComponent()
        {
            airplanes = new ConcurrentDictionary<string, Airplane>();
        }

        public void Start()
        {
            MqClient = new RabbitMqClient();
            CreateQueues();
            DeclareQueues();
            MqClient.PurgeQueues();
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
            MqClient.DeclareQueues(queuesTo.Values.ToArray());
            MqClient.DeclareQueues(queuesFrom.Values.ToArray());
        }
        void Subscribe()
        {
            MqClient.SubscribeTo<AirplaneGenerationRequest>(queuesFrom[Component.Schedule], mes =>   //schedule
                     ScheduleResponse(mes));
            MqClient.SubscribeTo<PassengerTransferRequest>(queuesFrom[Component.Bus], mes =>    //bus
                     BusTransferResponse(mes));
            MqClient.SubscribeTo<BaggageTransferRequest>(queuesFrom[Component.Baggage], mes =>  //baggage
                     BaggageTransferResponse(mes));
            MqClient.SubscribeTo<NewTimeSpeedFactor>(queuesFrom[Component.TimeService], mes =>  //time speed
                     TimeSpeedFactor = mes.Factor);
            MqClient.SubscribeTo<FollowMeCommand>(queuesFrom[Component.FollowMe], mes =>  //follow me
                     FollowAction(mes));
            MqClient.SubscribeTo<DeicingCompletion>(queuesFrom[Component.Deicing], mes =>   //deicing
                     airplanes[mes.PlaneId].IsDeiced = true);
            MqClient.SubscribeTo<RefuelCompletion>(queuesFrom[Component.FuelTruck], mes =>  //fueltruck
                     airplanes[mes.PlaneId].FuelAmount += mes.Fuel);
            MqClient.SubscribeTo<CateringCompletion>(queuesFrom[Component.Catering], mes => //catering
                     airplanes[mes.PlaneId].FoodList = mes.FoodList);
            MqClient.SubscribeTo<DepartureSignal>(queuesFrom[Component.GroundService], mes =>   //groundservice
                     Departure(mes));

        }
        void ScheduleResponse(AirplaneGenerationRequest req)
        {
            Airplane airplane = Generator.Generate(req.AirplaneModelName, req.FlightId);
            if (airplanes.TryAdd(airplane.PlaneID, airplane))
            {
                MqClient.Send<AirplaneGenerationResponse>(queuesTo[Component.Schedule],
                    new AirplaneGenerationResponse()
                    {
                        FlightId = req.FlightId,
                        PlaneId = airplane.PlaneID
                    });
            }
        }
        void BusTransferResponse(PassengerTransferRequest req)
        {
            Airplane plane = airplanes[req.PlaneId];
            if (req.Action == TransferAction.Take)
            {
                MqClient.Send<PassengersTransfer>(queuesTo[Component.Bus], new PassengersTransfer()
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
                MqClient.Send<BaggageTransfer>(queuesTo[Component.Baggage], new BaggageTransfer()
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
        void AirplaneServiceCommand(string planeID)
        {
            Airplane plane = airplanes[planeID];
            MqClient.Send<AirplaneServiceCommand>(queuesTo[Component.GroundService],
                new AirplaneServiceCommand()
                {
                    LocationVertex = plane.LocationVertex,
                    PlaneId = planeID,
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
            int timeInterval = 10;
            double position = 0;
            var plane = airplanes[cmd.PlaneId];
            int distance = GetDistance(plane.LocationVertex, cmd.DestinationVertex);
            SendVisualizationMessage(plane, cmd, Airplane.Speed);
            Task task = Task.Run(() =>
            {
                while (position < distance)
                {
                    position += Airplane.Speed * timeInterval * TimeSpeedFactor;
                    Thread.Sleep(10);
                };
                SendVisualizationMessage(plane, cmd, 0);
                MqClient.Send<ArrivalConfirmation>(queuesTo[Component.FollowMe], new ArrivalConfirmation()
                {
                    PlaneId = plane.PlaneID,
                    FollowMeId = cmd.FollowMeId,
                    LocationVertex = plane.LocationVertex
                });
                plane.LocationVertex = cmd.DestinationVertex;
            });
        }

        void SendVisualizationMessage(Airplane plane, FollowMeCommand cmd, int speed)
        {
            MqClient.Send<VisualizationMessage>(queuesTo[Component.Visualizer], new VisualizationMessage()
            {
                StartVertex = plane.LocationVertex,
                DestinationVertex = cmd.DestinationVertex,
                Speed = speed,
                ObjectId = plane.PlaneID,
                Type = plane.Model.Model
            });
        }

        int GetDistance(int locationVertex, int destinationVertex)    //TODO will return distance from graph library
        {
            return 0;
        }
        void Departure(DepartureSignal signal)    //TODO departure signal
        {

        }
    }
}

