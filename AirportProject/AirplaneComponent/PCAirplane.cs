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
        string[] queues = new string[] { "Aiplane-Schedule" };
        public PCAirplane()
        {
            MqClient = new RabbitMqClient();
        }

        void SubscribeAll()
        {
            MqClient.SubscribeTo<AirplaneGenerationRequest>(queues[0], mes =>
                     CreateScheduleResponse(mes));
        }
        AirplaneGenerationResponse CreateScheduleResponse(AirplaneGenerationRequest req)
        {
            Airplane airplane = Generator.Generate(req.AirplaneModelName, req.FlightId);
            airplanes.Add(airplane.AirplaneID, airplane);
            return new AirplaneGenerationResponse()
            {
                FlightId = req.FlightId,
                PlaneId = airplane.AirplaneID
            };
        }
    } 
}
