using System;
using System.Collections.Generic;
using System.Text;
using RabbitMqWrapper;
using AirportLibrary;
using AirportLibrary.DTO;

namespace FuelTruck
{
    public class FuelTruck
    {
        //Получает сообщения
        const string queueFromTimeService = Component.TimeService + Component.FuelTruck;
        const string queueFromGroundService = Component.GroundService + Component.FuelTruck;
        const string queueFromGroundMotion = Component.GroundMotion + Component.FuelTruck;

        //Отправляет сообщения
        const string queueToAirPlane = Component.FuelTruck + Component.Airplane;
        const string queueToLogs = Component.FuelTruck + Component.Logs;
        const string queueToGroundMotion = Component.FuelTruck + Component.GroundMotion;
        const string queueToGroundService = Component.FuelTruck + Component.GroundService;
        const string queueToVisualizer = Component.FuelTruck + Component.Visualizer;

        public RabbitMqClient mqClient = new RabbitMqClient();
        
        public double timeCoef { get; set; } = 1.0;
        public int motionInterval = 100; //ms
        public string planeID;
        public int planeLocationVertex;
        public int fuel;
        public string logMessage = "Проинициализировались";

        //Проверка свободных машинок
        public int CheckFreeFuelTruckCar()
        {
            int numOfCar = -1;
            return numOfCar;
        }
        //необходима постоянная проверка времени
        public void FuelTruckJob(string planeID, int fuel, int planeLocationVertex)
        {
            //запрашивает номер свободной машины
            //делает статус этой машины занятая
            //едет заправлять самолет         

        }

        public void SendLogMessage(string message)
        {
            var newLogMessage = new LogMessage()
            {
                Message = message,
                Component = "FuelTruck"
            };
            mqClient.Send(queueToLogs, newLogMessage);
        }

        public void Start()
        {
            //объявление запросов
            mqClient.DeclareQueues(queueFromTimeService, queueFromGroundService, queueFromGroundMotion,
                queueToAirPlane, queueToLogs, queueToGroundMotion, queueToGroundService, queueToVisualizer);

            //время
            mqClient.SubscribeTo<NewTimeSpeedFactor>(queueFromTimeService, (mes) =>
            {
                timeCoef = mes.Factor;
                logMessage = "Коэффициент времени изменился и стал: " + timeCoef;
                SendLogMessage(logMessage);
                Console.WriteLine(logMessage);
            });

            mqClient.SubscribeTo<RefuelServiceCommand>(queueFromGroundService, (mes) =>
            {
                FuelTruckJob(mes.PlaneId, mes.Fuel, mes.PlaneLocationVertex);
                logMessage = String.Format("Запустили машинку в путь, PlaneID: " +
                    "{0}, Fuel: {1}, PlaneLocation: {2}", mes.PlaneId, mes.Fuel, mes.PlaneLocationVertex);
                SendLogMessage(logMessage);
                Console.WriteLine(logMessage);

            });




        }

        
    }
}
