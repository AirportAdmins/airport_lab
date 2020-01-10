using System;
using RabbitMqWrapper;
using AirportLibrary;
using AirportLibrary.DTO;



namespace FuelTruckComponent
{
    class FuelTruck
    {
        //Получает сообщения
        const string queueFromTimeService = Component.TimeService + Component.FuelTruck;
        const string queueFromGroundService = Component.GroundService + Component.FuelTruck;
        const string queueFromGroundMotion = Component.GroundMotion + Component.GroundMotion;

        //Отправляет сообщения
        const string queueToAirPlane = Component.FuelTruck + Component.Airplane;
        const string queueToLogs = Component.FuelTruck + Component.Logs;
        const string queueToGroundMotion = Component.FuelTruck + Component.GroundMotion;
        const string queueToGroundService = Component.FuelTruck + Component.GroundService;

        public RabbitMqClient mqClient { get; set; } = new RabbitMqClient();
        public double timeCoef { get; set; } = 1;
        public string planeID;
        public int fuel;
        static void Main(string[] args)
        {

            var ft = new FuelTruck();

            ft.mqClient.DeclareQueues(queueFromTimeService, queueFromGroundService, queueFromGroundMotion,
                queueToAirPlane, queueToLogs, queueToGroundMotion, queueToGroundService);

            ft.mqClient.SubscribeTo<NewTimeSpeedFactor>(queueFromTimeService, (mes) =>
            {
                ft.timeCoef = mes.Factor;
                Console.WriteLine("Коэффициент времени изменился и стал: " + ft.timeCoef);
            });

            //НУЖНО ДОБАВИТЬ МЕСТО САМОЛЕТА В ДТО
            ft.mqClient.SubscribeTo<RefuelCompletion>(queueFromGroundService, (mes) =>
            {
                ft.FuelTruckJob(mes.PlaneId, mes.Fuel);
                Console.WriteLine("Топливозаправщик поехал");
                //ft.planeID = mes.PlaneId;
                //ft.fuel = mes.Fuel;
                //airplanePos = mes.
            });

            //ft.mqClient.Dispose();
        }
        
        //Работа Топливозаправщика
        public void FuelTruckJob(string planeID, int fuel)
        {
            //постоянная проверка времени

        }

        //что с графом???
        //Топливозаправщиков несколько и они инициализируются в самом начале? 
    }
}
