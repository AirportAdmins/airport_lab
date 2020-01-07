using System;
using System.Collections.Generic;
using System.Text;
using RabbitMqWrapper;
using AirplaneComponent.AirplaneGenerator;
using AirportLibrary.DTO;

namespace AirplaneComponent
{
    public class PCAirplane
    {
        RabbitMqClient MqClient;
        Dictionary<string, Airplane> airplanes;
        string[] queues = new string[] { "Aiplane-Schedule","Airplane-Bus" };
        public PCAirplane()
        {
            MqClient = new RabbitMqClient();
        }

        void SubscribeAll()
        {
            MqClient.SubscribeTo<AirplaneGenerationRequest>(queues[0], mes =>   //schedule
                     ScheduleResponse(mes));
            MqClient.SubscribeTo<PassengerTransferRequest>(queues[1], mes =>    //bus
                     BusTransferResponse(mes));
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
            if (req.Action == TransferAction.Take)
            {
                MqClient.Send<PassengersTransfer>(queues[1], new PassengersTransfer()
                {
                    BusId = req.BusId,
                    PassengerCount = airplanes[req.PlaneId].Passengers
                });
                airplanes[req.PlaneId].Passengers = 0;
            }
            else
            {
                airplanes[req.PlaneId].Passengers = req.PassengersCount;
            }
        }
    } 
}
