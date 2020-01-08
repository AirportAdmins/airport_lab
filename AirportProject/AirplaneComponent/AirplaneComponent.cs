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
            MqClient = new RabbitMqClient();
        }

        void FillQueues()
        {
            queues = new Dictionary<string, string>();
            queues.Add(Component.Schedule, Component.Airplane + Component.Schedule);
            queues.Add(Component.Bus, Component.Airplane + Component.Bus);
            queues.Add(Component.Baggage, Component.Airplane + Component.Baggage);
            queues.Add(Component.GroundService, Component.Airplane + Component.GroundService);
        }
        
        void DeclareQueues()
        {
            string[] queuesString = new string[3];
            queues.Values.CopyTo(queuesString,0);
            MqClient.DeclareQueues(queuesString);
        }
        void SubscribeAll()
        {
            MqClient.SubscribeTo<AirplaneGenerationRequest>(queues[Component.Schedule], mes =>   //schedule
                     ScheduleResponse(mes));
            MqClient.SubscribeTo<PassengerTransferRequest>(queues[Component.Bus], mes =>    //bus
                     BusTransferResponse(mes));
            MqClient.SubscribeTo<BaggageTransferRequest>(queues[Component.Baggage], mes =>    //baggage
                     BaggageTransferResponse(mes));
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
                    Needs=new List<Tuple<AirplaneNeeds, int>>(){
                        new Tuple<AirplaneNeeds, int> (AirplaneNeeds.PickUpPassengers,plane.Passengers),
                        new Tuple<AirplaneNeeds, int> (AirplaneNeeds.PickUpPassengers,plane.Passengers),

                    }
                });
        }                  
    } 
}
