using System;
using System.Collections.Generic;
using System.Text;
using RabbitMqWrapper;
using AirplaneComponent.AirplaneGenerator;
using AirportLibrary.DTO;
using AirportLibrary;

namespace AirplaneComponent
{
    public class AirplaneComponent
    {
        RabbitMqClient MqClient;
        Dictionary<string, Airplane> airplanes;
        Dictionary<string, string> queues;
        public AirplaneComponent()
        {
            airplanes = new Dictionary<string, Airplane>();
            MqClient = new RabbitMqClient();
        }

        void FillQueues()
        {
            queues = new Dictionary<string, string>()
            {
                { Component.Schedule, Component.Airplane + Component.Schedule },
                { Component.Bus, Component.Airplane + Component.Bus },
                { Component.Baggage, Component.Airplane + Component.Baggage },
                { Component.GroundService, Component.Airplane + Component.GroundService },
                { Component.TimeService, Component.Airplane + Component.TimeService }
            };
        }
        
        void DeclareQueues()
        {
            string[] queuesString = new string[3];
            queues.Values.CopyTo(queuesString,0);
            MqClient.DeclareQueues(queuesString);
        }
        void Subscribe()
        {
            MqClient.SubscribeTo<AirplaneGenerationRequest>(queues[Component.Schedule], mes =>   //schedule
                     ScheduleResponse(mes));
            MqClient.SubscribeTo<PassengerTransferRequest>(queues[Component.Bus], mes =>    //bus
                     BusTransferResponse(mes));
            MqClient.SubscribeTo<BaggageTransferRequest>(queues[Component.Baggage], mes =>    //baggage
                     BaggageTransferResponse(mes));
            MqClient.SubscribeTo<NewTimeSpeedFactor>(queues[Component.TimeService], mes =>  //time speed
                     TimeSpeedChanged(mes));
            
        }
        void ScheduleResponse(AirplaneGenerationRequest req)
        {
            Airplane airplane = Generator.Generate(req.AirplaneModelName, req.FlightId);
            airplanes.Add(airplane.PlaneID, airplane);
            MqClient.Send<AirplaneGenerationResponse>(queues[Component.Schedule],
                new AirplaneGenerationResponse() 
                { 
                    FlightId = req.FlightId,
                    PlaneId = airplane.PlaneID
                });
        }
        void BusTransferResponse(PassengerTransferRequest req)
        {
            Airplane plane = airplanes[req.PlaneId];
            if (req.Action == TransferAction.Take)
            {
                MqClient.Send<PassengersTransfer>(queues[Component.Bus], new PassengersTransfer()
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
                MqClient.Send<BaggageTransfer>(queues[Component.Baggage], new BaggageTransfer()
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
            MqClient.Send<AirplaneServiceCommand>(queues[Component.GroundService],
                new AirplaneServiceCommand()
                {
                    LocationVertex=plane.MotionData.LocationVertex,
                    PlaneId=planeID,
                    Needs=new List<Tuple<AirplaneNeeds, int>>()
                    {
                        Tuple.Create(AirplaneNeeds.PickUpPassengers,plane.Passengers),
                        Tuple.Create(AirplaneNeeds.PickUpBaggage,plane.BaggageAmount),
                        Tuple.Create(AirplaneNeeds.Refuel,plane.Model.Fuel-plane.FuelAmount)
                    }
                });
        }                  
        void TimeSpeedChanged(NewTimeSpeedFactor factor)
        {
            foreach(var plane in airplanes.Values)
            {
                plane.MotionData.Speed *= factor.Factor;
            }
        }

    } 
}
