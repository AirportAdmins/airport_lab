using System;
using RabbitMqWrapper;
using AirportLibrary;
using AirportLibrary.DTO;

namespace BaggageComponent
{
    class Baggage
    {
        
        const string queueFromTimeService = Component.TimeService + Component.Baggage;
        const string queueFromGroundService = Component.GroundService + Component.Baggage;
        const string queueFromGroundMotion = Component.GroundMotion + Component.GroundMotion;
        const string queueFromStorage = Component.Storage + Component.Baggage;



        const string queueToAirPlane = Component.Baggage + Component.Airplane;
        const string queueToLogs = Component.Baggage + Component.Logs;
        const string queueToGroundMotion = Component.Baggage + Component.GroundMotion;
        const string queueToGroundService = Component.Baggage + Component.GroundService;

        public RabbitMqClient mqClient { get; set; } = new RabbitMqClient();
        public double timeCoef { get; set; } = 1;
        public string planeID;
        public int baggage;
        static void Main(string[] args)
        {

            var ft = new Baggage();

            ft.mqClient.DeclareQueues(queueFromTimeService, queueFromGroundService, queueFromGroundMotion,
                queueToAirPlane, queueToLogs, queueToGroundMotion, queueToGroundService);

            ft.mqClient.SubscribeTo<NewTimeSpeedFactor>(queueFromTimeService, (mes) =>
            {
                ft.timeCoef = mes.Factor;
                Console.WriteLine("Коэффициент времени изменился и стал: " + ft.timeCoef);
            });

            

    
        }
        public void BaggageJob(string planeID, int bagCount)
        {
            

        }

        
    }
}
