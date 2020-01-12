using System;
using RabbitMqWrapper;
using AirportLibrary;
using AirportLibrary.DTO;
using System.Collections.Generic;
namespace BaggageComponent
{
    class Baggage
    {
        
        const string queueFromTimeService = Component.TimeService + Component.Baggage;
        const string queueFromGroundService = Component.GroundService + Component.Baggage;
        const string queueFromGroundMotion = Component.GroundMotion + Component.Baggage;
        const string queueFromStorage = Component.Storage + Component.Baggage;
        const string queueFromAirPlane = Component.Airplane + Component.Baggage;


        const string queueToAirPlane = Component.Baggage + Component.Airplane;
        const string queueToLogs = Component.Baggage + Component.Logs;
        const string queueToGroundMotion = Component.Baggage + Component.GroundMotion;
        const string queueToGroundService = Component.Baggage + Component.GroundService;
        const string queuetoStorage = Component.Baggage + Component.Storage;

        public RabbitMqClient mqClient { get; set; } = new RabbitMqClient();
        public double timeCoef { get; set; } = 1;
        public string planeID;
        public int baggageCount;
        public string BcarId;
        public int StorVertex;
        public int PlaneVertex;
        public static readonly List<string> queues = new List<string>
        {
            queueFromTimeService, queueFromGroundService, queueFromGroundMotion,  queueFromStorage ,queueToAirPlane, queueToLogs, queueToGroundMotion,queueToGroundService, queuetoStorage,queueFromAirPlane
        };



        static void Main(string[] args)
        {

            var bg = new Baggage();
             bg.mqClient.DeclareQueues(queues.ToArray());//обьявление
             bg.mqClient.PurgeQueues(queues.ToArray());//очистка

            bg.mqClient.DeclareQueues(queueFromTimeService, queueFromGroundService, queueFromGroundMotion,
                queueToAirPlane, queueToLogs, queueToGroundMotion, queueToGroundService);

            bg.mqClient.SubscribeTo<NewTimeSpeedFactor>(queueFromTimeService, (mes) =>
            {
                bg.timeCoef = mes.Factor;
                Console.WriteLine("Коэффициент времени изменился и стал: " + bg.timeCoef);
            });

            //bg.mqClient.SubscribeTo<FlightStorageInfoResponse>(queueFromStorage, (mes) =>
            //{
            //    bg.planeID= mes.FlightId;
            //    bg.baggageCount = mes.BaggageCount;              
            //});

            bg.mqClient.SubscribeTo<BaggageTransfer>(queueFromAirPlane, (mes) =>
            {
                bg.BcarId = mes.BaggageCarId;
                bg.baggageCount = mes.BaggageCount;
            });
            bg.mqClient.SubscribeTo<BaggageServiceCommand>(queueFromGroundService, (mes) =>
            {
                bg.StorVertex = mes.StorageVertex;
                bg.PlaneVertex = mes.PlaneLocationVertex;
                bg.planeID = mes.FlightId;
                bg.baggageCount = mes.BaggageCount;
            });
        }
        // ответ 







        public void BaggageJob(string planeID, int bagCount)
        {
            

        }

        
    }
}
