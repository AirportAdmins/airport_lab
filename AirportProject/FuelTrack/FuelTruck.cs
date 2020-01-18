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
        const string queueFromTimeService = Component.TimeService + Component.FuelTruck; //Так ли получать сообщения? или как у СНД?
        const string queueFromGroundService = Component.GroundService + Component.FuelTruck; //рэди
        const string queueFromGroundMotion = Component.GroundMotion + Component.FuelTruck;

        //Отправляет сообщения
        const string queueToAirPlane = Component.FuelTruck + Component.Airplane;
        const string queueToLogs = Component.FuelTruck + Component.Logs; //так отправлять логи? или как у СНД?
        const string queueToGroundMotion = Component.GroundMotion;
        const string queueToGroundService = Component.FuelTruck + Component.GroundService;
        const string queueToVisualizer = Component.FuelTruck + Component.Visualizer; //ready

        Map mapAirport = new Map();
        
        public double timeCoef { get; set; } = 1.0;
        public RabbitMqClient mqClient;
        Dictionary<int, FuelTruckCars> cars;
        //public int motionInterval = 100; //ms
        public string planeID;
        public int planeLocationVertex;
        public int fuel;
        const int carCount = 3; //Количество машинок
        public string logMessage = "Проинициализировались, кол-во машинок: " + carCount;


        //Конструктор. Создаем машинки
        public FuelTruck()
        {
            mqClient = new RabbitMqClient();
            Dictionary<int, FuelTruckCars> cars = new Dictionary<int, FuelTruckCars>();
            //переделать в словарь
            for (int i=1; i <= carCount; i++)
            {
                cars.Add(i, new FuelTruckCars() { intCarID = i, CarID = "FuelTruck #" + i,
                    Position = 19, Status = Status.Free, FuelOnBoard = FuelTruckCars.MaxFuelOnBoard});
            }
        }    //ВАЖНАЯ ВЕЩЬ. В ДАННЫЙ МОМЕНТ НЕТ НЕОБХОДИМОСТИ В КОЛ-ВЕ ТОПЛИВА, Т.К. ОДИН МАШИНКА ЗАПРАВЛЯЕТ ОДИН САМОЛЕТ И ПОТОМ ЕДЕТ ДОМОЙ          

        //Проверка свободных машинок. Возвращает id машинки. СНАЧАЛА ВСЕ МАШИНКИ ЕДУТ ДОМОЙ
        public int CheckFreeFuelTruckCar(int fuel)
        {
            int numOfCar = -1;
            while (true)
            {                
                foreach (FuelTruckCars ftc in cars.Values)
                {
                    if ((ftc.Status == Status.Free) & (ftc.FuelOnBoard > fuel)) //Free
                    {
                        numOfCar = ftc.intCarID;
                        ftc.Status = Status.Busy;
                    }
                }
                if (numOfCar == -1)
                {
                    /* ВАЖНО ПОНЯТЬ СКОЛЬКО ОЖИДАТЬ И КАК ЧАСТО ОТПРАВЛЯТЬ ЛОГИ */
                    logMessage = "Все машины заняты, подождем 5 секунд * коэф. времени";                    
                    SendLogMessage(logMessage);
                    Thread.Sleep(Convert.ToInt32(5000 * timeCoef));
                }
            }
            return numOfCar;
        }

        //Проверяем свободен ли граф
        public bool canFuelTruckGo(int f1rs, int s3cond, int currCar, int status) //0-полная, 1 done
        {
            bool yesNo = false;
            var newMotionPermissionRequest = new MotionPermissionRequest()
            {
                Component = "FuelTruck",
                ObjectId = cars[currCar].CarID,
                StartVertex = f1rs,
                DestinationVertex = s3cond,
                Action = MotionAction.Occupy,
            };
            mqClient.Send(queueToGroundMotion, newMotionPermissionRequest);
            
            ///////////тут я получаю ответ
            
            return yesNo;
        }



        //необходима постоянная проверка времени
        public void FuelTruckJob(string planeID, int fuel, int planeLocationVertex)
        {
            int currentlyCar;
            currentlyCar = CheckFreeFuelTruckCar(fuel); //вычислил ID свободной машинки

            var shortCut = mapAirport.FindShortcut(cars[currentlyCar].Position, planeLocationVertex);//Нашел кротчайший путь
            shortCut.Add(1000);
            foreach (int i in shortCut)
            {

            }
        }

        //Отправка сообщений визуализатору
        public void SendVisualizerMessage(string objectID, int startVertex, int destinationVertex, int speed) //возможно сделать все на +-?
        {
            var newVisMessage = new VisualizationMessage()
            {
                Type = Component.FuelTruck,
                ObjectId = objectID,
                StartVertex = startVertex,
                DestinationVertex = destinationVertex,
                Speed = speed         ///////////////speeeeed??????
            };
            mqClient.Send(queueToVisualizer, newVisMessage);

        }

        //Отправка ЛОГ сообщений
        public void SendLogMessage(string message)
        {
            var newLogMessage = new LogMessage()
            {
                Message = message,
                Component = "FuelTruck"
            };
            Console.WriteLine(message);
            mqClient.Send(queueToLogs, newLogMessage);            
        }

        //Наше начало и подписки на время и СНО
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
            });

            mqClient.SubscribeTo<RefuelServiceCommand>(queueFromGroundService, (mes) =>
            {
                FuelTruckJob(mes.PlaneId, mes.Fuel, mes.PlaneLocationVertex);
                logMessage = String.Format("Запустили машинку в путь, PlaneID: " +
                    "{0}, Fuel: {1}, PlaneLocation: {2}", mes.PlaneId, mes.Fuel, mes.PlaneLocationVertex);
                SendLogMessage(logMessage);
            });
        }

        
    }
}
