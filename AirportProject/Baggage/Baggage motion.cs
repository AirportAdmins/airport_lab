using System;
using RabbitMqWrapper;
using AirportLibrary;
using AirportLibrary.DTO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using AirportLibrary.Graph;
using System.Collections.Concurrent;
using AirportLibrary.Delay;

namespace Baggage
{
    /// <summary>
    /// Здесь описаны методы, которые нужны всем машинам.
    /// Главный метод -- Start().
    /// </summary>
    public partial class Baggage
    {
        delegate void GoToVertexAction(BaggageCar baggage, int DestinationVertex);

        PlayDelaySource source;

        ConcurrentDictionary<string, BaggageCar> cars;
        int countOfCars = 6;
        ConcurrentDictionary<string, CancellationTokenSource> tokens;
        Map map = new Map();

        double TimeSpeedFactor = 1;
        int motionInterval = 100;       //ms

        int storageVertex = 25;


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
        const string queueToVisualizer = Component.Baggage + Component.Visualizer;

        public RabbitMqClient mqClient;

        public readonly List<string> queues = new List<string>
        {
            queueFromTimeService, queueFromGroundService, queueFromGroundMotion,  queueFromStorage, queueFromAirPlane, queueToAirPlane, queueToLogs, queueToGroundMotion,queueToGroundService, queuetoStorage, queueToVisualizer
        };

        public Baggage()
        {
            cars = new ConcurrentDictionary<string, BaggageCar>();
            tokens = new ConcurrentDictionary<string, CancellationTokenSource>();
            mqClient = new RabbitMqClient("v174153.hosted-by-vdsina.ru", "groundservice", "5254");
            source = new PlayDelaySource(TimeSpeedFactor);
        }

        public void Start()
        {
            DeclareQueues();
            FillCollections();
            Subscribe();
        }

        private void FillCollections()
        {
            for (int i = 0; i < countOfCars; i++)
            {
                BaggageCar car = new BaggageCar(i);
                cars.TryAdd(car.BaggageCarID, car);
            }
        }

        private void Subscribe()
        {
            TakeBaggageFromPlane();
            TakeBaggageFromStorage();
            MessageFromGroundService();
            MotionPermissionResponse();
            TakeTimeSpeedFactor();
        }

        private void DeclareQueues()
        {
            mqClient.DeclareQueues(queues.ToArray());
        }

        // ответ 
        private void GoPath(GoToVertexAction action, BaggageCar baggageCar, int destinationVertex)
        {
            var path = map.FindShortcut(baggageCar.LocationVertex, destinationVertex);
            for (int i = 0; i < path.Count - 1; i++)
            {
                action(baggageCar, path[i + 1]);
            }
        }
        private void GoPathHome(BaggageCar baggageCar, int destinationVertex,
        CancellationTokenSource cancellationToken)
        {
            var path = map.FindShortcut(baggageCar.LocationVertex, destinationVertex);

            baggageCar.Status = Status.Free;

            for (int i = 0; i < path.Count - 1; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                GoToVertexAlone(baggageCar, path[i + 1]);
            }
        }
        private void GoToVertexAlone(BaggageCar baggageCar, int DestinationVertex)
        {
            WaitForMotionPermission(baggageCar, DestinationVertex);
            MakeAMove(baggageCar, DestinationVertex);
            mqClient.Send<MotionPermissionRequest>(queueToGroundMotion, //free edge
            new MotionPermissionRequest()
            {
                Action = MotionAction.Free,
                DestinationVertex = DestinationVertex,
                Component = Component.Baggage,
                ObjectId = baggageCar.BaggageCarID,
                StartVertex = baggageCar.LocationVertex
            });
        }
        private void WaitForMotionPermission(BaggageCar baggageCar, int DestinationVertex)
        {
            mqClient.Send<MotionPermissionRequest>(Component.Baggage, //permission request
                new MotionPermissionRequest()
                {
                    Action = MotionAction.Occupy,
                    Component = Component.Baggage,
                    DestinationVertex = DestinationVertex,
                    ObjectId = baggageCar.BaggageCarID,
                    StartVertex = baggageCar.LocationVertex
                });

            while (!baggageCar.MotionPermitted)               //check if baggacar can go
                source.CreateToken().Sleep(5);
        }

        private void MotionPermissionResponse()
        {
            mqClient.SubscribeTo<MotionPermissionResponse>(queueFromGroundMotion, (mpr) =>
            {
                cars[mpr.ObjectId].MotionPermitted = true;
            });
        }

        private void MakeAMove(BaggageCar baggageCar, int DestinationVertex)     //just move to vertex
        {
            double position = 0;
            int distance = map.Graph.GetWeightBetweenNearVerties(baggageCar.LocationVertex, DestinationVertex);
            SendVisualizationMessage(baggageCar, DestinationVertex, BaggageCar.Speed);
            while (position < distance)                     //go
            {
                position += BaggageCar.Speed / 3.6 / 1000 * motionInterval * TimeSpeedFactor;
                source.CreateToken().Sleep(motionInterval);
            };
            SendVisualizationMessage(baggageCar, DestinationVertex, 0);
            baggageCar.LocationVertex = DestinationVertex;
            baggageCar.MotionPermitted = false;
        }
        private void SendVisualizationMessage(BaggageCar baggageCar, int DestinationVertex, int speed)
        {
            mqClient.Send<VisualizationMessage>(queueToVisualizer, new VisualizationMessage()
            {
                ObjectId = baggageCar.BaggageCarID,
                DestinationVertex = DestinationVertex,
                Speed = speed,
                StartVertex = baggageCar.LocationVertex,
                Type = Component.Baggage
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
