using AirportLibrary;
using AirportLibrary.Delay;
using AirportLibrary.DTO;
using RabbitMqWrapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DeicingComponent
{
    partial class DeicingComponent
    {
        delegate void GoToVertexAction(DeicingCar deicingCar, int DestinationVertex);

        PlayDelaySource source;

        ConcurrentDictionary<string, DeicingCar> cars;
        int countOfCars = 2;
        ConcurrentDictionary<string, CancellationTokenSource> tokens;
        Map map = new Map();

        double TimeSpeedFactor = 1;
        int motionInterval = 100;       //ms


        const string queueFromTimeService = Component.TimeService + Component.Deicing;
        const string queueFromGroundService = Component.GroundService + Component.Deicing;
        const string queueFromGroundMotion = Component.GroundMotion + Component.Deicing;

        const string queueToAirPlane = Component.Deicing + Component.Airplane;
        const string queueToLogs = Component.Logs;
        const string queueToGroundMotion = Component.GroundMotion;
        const string queueToGroundService = Component.Deicing + Component.GroundService;
        const string queueToVisualizer = Component.Visualizer;

        public RabbitMqClient mqClient;

        public readonly List<string> queues = new List<string>
        {
            queueFromTimeService, queueFromGroundService, queueFromGroundMotion, queueToAirPlane, queueToLogs, queueToGroundMotion,queueToGroundService, queueToVisualizer
        };


        public DeicingComponent()
        {
            cars = new ConcurrentDictionary<string, DeicingCar>();
            tokens = new ConcurrentDictionary<string, CancellationTokenSource>();
            mqClient = new RabbitMqClient();
            source = new PlayDelaySource(TimeSpeedFactor);
        }

        public void Start()
        {
            Console.WriteLine($"{Component.Deicing} начал работу");
            DeclarePurgeQueues();
            FillCollections();
            Subscribe();
        }

        private void FillCollections()
        {
            for (int i = 0; i < countOfCars; i++)
            {
                DeicingCar car = new DeicingCar(i);
                cars.TryAdd(car.DeicingCarID, car);
            }
        }

        private void Subscribe()
        {
            MessageFromGroundService();
            MotionPermissionResponse();
            TakeTimeSpeedFactor();
        }

        private void DeclarePurgeQueues()
        {
            mqClient.DeclareQueues(queues.ToArray());
            mqClient.PurgeQueues(queues.ToArray());
        }

        // ответ 
        private void GoPath(GoToVertexAction action, DeicingCar deicingCar, int destinationVertex)
        {
            var path = map.FindShortcut(deicingCar.LocationVertex, destinationVertex);
            Console.WriteLine($"{deicingCar.DeicingCarID} поедет из {path[0]} в {path[path.Count - 1]}");
            for (int i = 0; i < path.Count - 1; i++)
            {
                action(deicingCar, path[i + 1]);
            }
        }
        private void GoPathHome(DeicingCar deicingCar, int destinationVertex,
        CancellationTokenSource cancellationToken)
        {
            var path = map.FindShortcut(deicingCar.LocationVertex, destinationVertex);
            Console.WriteLine($"{deicingCar.DeicingCarID} поедет домой из {path[0]} в {path[path.Count - 1]}");
            deicingCar.Status = Status.Free;

            for (int i = 0; i < path.Count - 1; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                GoToVertexAlone(deicingCar, path[i + 1]);
            }
        }
        private void GoToVertexAlone(DeicingCar deicingCar, int DestinationVertex)
        {
            WaitForMotionPermission(deicingCar, DestinationVertex);
            int StartVertex = deicingCar.LocationVertex;
            MakeAMove(deicingCar, DestinationVertex);
            mqClient.Send<MotionPermissionRequest>(queueToGroundMotion, //free edge
            new MotionPermissionRequest()
            {
                Action = MotionAction.Free,
                DestinationVertex = DestinationVertex,
                Component = Component.Deicing,
                ObjectId = deicingCar.DeicingCarID,
                StartVertex = StartVertex
            });
        }
        private void WaitForMotionPermission(DeicingCar deicingCar, int DestinationVertex)
        {
            Console.WriteLine($"{deicingCar.DeicingCarID} ждёт разрешения передвинуться в {DestinationVertex}");
            mqClient.Send<MotionPermissionRequest>(queueToGroundMotion, //permission request
                new MotionPermissionRequest()
                {
                    Action = MotionAction.Occupy,
                    Component = Component.Deicing,
                    DestinationVertex = DestinationVertex,
                    ObjectId = deicingCar.DeicingCarID,
                    StartVertex = deicingCar.LocationVertex
                });

            while (!deicingCar.MotionPermitted)               //check if deicingcar can go
                source.CreateToken().Sleep(5);
        }

        private void MotionPermissionResponse()
        {
            mqClient.SubscribeTo<MotionPermissionResponse>(queueFromGroundMotion, (mpr) =>
            {
                Console.WriteLine($"{mpr.ObjectId} получил разрешение на перемещение");
                cars[mpr.ObjectId].MotionPermitted = true;
            });
        }

        private void MakeAMove(DeicingCar deicingCar, int DestinationVertex)     //just move to vertex
        {
            int distance = map.Graph.GetWeightBetweenNearVerties(deicingCar.LocationVertex, DestinationVertex);
            SendVisualizationMessage(deicingCar, DestinationVertex, DeicingCar.Speed);
            source.CreateToken().Sleep(distance * 1000 / DeicingCar.Speed);
            SendVisualizationMessage(deicingCar, DestinationVertex, 0);
            deicingCar.LocationVertex = DestinationVertex;
            deicingCar.MotionPermitted = false;
        }
        private void SendVisualizationMessage(DeicingCar deicingCar, int DestinationVertex, int speed)
        {
            mqClient.Send<VisualizationMessage>(queueToVisualizer, new VisualizationMessage()
            {
                ObjectId = deicingCar.DeicingCarID,
                DestinationVertex = DestinationVertex,
                Speed = speed,
                StartVertex = deicingCar.LocationVertex,
                Type = Component.Deicing
            });
        }

        /// <summary>
        /// Получаем новый коэффициент времени от службы времени
        /// </summary>
        private void TakeTimeSpeedFactor()
        {
            mqClient.SubscribeTo<NewTimeSpeedFactor>(queueFromTimeService, (ntsf) =>
            {
                TimeSpeedFactor = ntsf.Factor;
                source.TimeFactor = TimeSpeedFactor;
            });
        }
    }
}
