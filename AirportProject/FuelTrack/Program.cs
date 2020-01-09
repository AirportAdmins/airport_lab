using System;
using RabbitMqWrapper;
using AirportLibrary;
using AirportLibrary.DTO;

namespace FuelTrackComponent
{
    class FuelTrack
    {

        static void Main(string[] args)
        {
            //Получает сообщения
            var queueFromTimeService = Component.TimeService + Component.FuelTruck;
            var queueFromGroundService = Component.GroundService + Component.FuelTruck;
            var queueFromGroundMotion = Component.GroundMotion + Component.GroundMotion;

            //Отправляет сообщения
            var queueToAirPlane = Component.FuelTruck + Component.Airplane;
            var queueToLogs = Component.FuelTruck + Component.Logs;
            var queueToGroundMotion = Component.FuelTruck + Component.GroundMotion;
            var queueToGroundService = Component.FuelTruck + Component.GroundService;

            double timeCoef = 1.0;
            string planeID;
            int fuel;
            //var airplanePos = ;

            var mqClient = new RabbitMqClient();
            mqClient.DeclareQueues(queueFromTimeService, queueFromGroundService, queueFromGroundMotion,
                queueToAirPlane, queueToLogs, queueToGroundMotion, queueToGroundService);

            mqClient.SubscribeTo<NewTimeSpeedFactor>(queueFromTimeService, (mes) =>
            {
                timeCoef = mes.Factor;
            });

            //НУЖНО ДОБАВИТЬ МЕСТО САМОЛЕТА В ДТО
            mqClient.SubscribeTo<RefuelCompletion>(queueFromGroundService, (mes) =>
            {
                planeID = mes.PlaneId;
                fuel = mes.Fuel;
                //airplanePos = mes.
            });



            Console.WriteLine("Hello World!");
        }
    }
}
