using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using RabbitMqWrapper;
using AirportLibrary;
using AirportLibrary.DTO;
using System.Threading;

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

        
        public double timeCoef { get; set; } = 1.0;
        public RabbitMqClient mqClient;
        Dictionary<int, FuelTruckCars> cars;
        //public int motionInterval = 100; //ms
        public string planeID;
        public int planeLocationVertex;
        public int fuel;
        const int carCount = 3; //Количество машинок
        public string logMessage = "Проинициализировались, кол-во машинок: " + carCount;

        public FuelTruck()
        {
            mqClient = new RabbitMqClient();
            Dictionary<int, FuelTruckCars> cars = new Dictionary<int, FuelTruckCars>();
            //переделать в словарь
            for (int i=1; i <= carCount; i++)
            {
                cars.Add(i, new FuelTruckCars() { CarID = "FuelTruck #" + i, Position = 19 });
            }

        }        
        

        

        //Проверка свободных машинок. Возвращает id машинки
        public string CheckFreeFuelTruckCar()
        {
            string numOfCar = "ALL BUSY";
            while (true)
            {                
                foreach (FuelTruckCars ftc in cars.Values)
                {
                    if (ftc.Status == Status.Free) //Free
                    {
                        numOfCar = ftc.CarID;
                        ftc.Status = Status.Busy;
                    }
                }
                if (numOfCar == "ALL BUSY")
                {
                    /* ВАЖНО ПОНЯТЬ СКОЛЬКО ОЖИДАТЬ И КАК ЧАСТО ОТПРАВЛЯТЬ ЛОГИ */
                    logMessage = "Все машины заняты, подождем 5 секунд * коэф. времени";
                    Console.WriteLine(logMessage);
                    SendLogMessage(logMessage);
                    Thread.Sleep(Convert.ToInt32(5000 * timeCoef));
                }
            }
            return numOfCar;
        }
        //необходима постоянная проверка времени
        public void FuelTruckJob(string planeID, int fuel, int planeLocationVertex)
        {
            string currentlyCar;
            currentlyCar = CheckFreeFuelTruckCar();
            
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

            SendLogMessage(logMessage);

            
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
