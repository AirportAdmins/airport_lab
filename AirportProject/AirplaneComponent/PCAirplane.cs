using System;
using System.Collections.Generic;
using System.Text;
using RabbitMqWrapper;
using AirplaneComponent.AirplaneGenerator;
using AirportLibrary.DTO;
using AirportLibrary;

namespace AirplaneComponent
{
    public class PCAirplane
    {
        RabbitMqClient MqClient;
        Dictionary<string, Airplane> airplanes;
        Dictionary<string, string> queues;
        public PCAirplane()
        {
            MqClient = new RabbitMqClient();
        }

        void FillQueues()
        {
            queues = new Dictionary<string, string>();
            queues.Add(Component.Schedule, Component.Airplane + "-" + Component.Schedule);
            queues.Add(Component.Bus, Component.Airplane + "-" + Component.Bus);
            queues.Add(Component.Baggage, Component.Airplane + "-" + Component.Baggage);
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
            MqClient.Send<AirplaneGenerationResponse>(queues[0],new AirplaneGenerationResponse() 
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
    } 
}
