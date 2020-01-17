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

namespace Baggage
{
    public partial class Baggage
    {
        delegate void GoToVertexAction(BaggageCar baggage, int DestinationVertex);


        List<BaggageCar> cars;
        ConcurrentDictionary<string, CancellationTokenSource> tokens;
        Map map = new Map();

        double TimeSpeedFactor = 1;
        int commonIdCounter = 0;
        int motionInterval = 100;       //ms
        public Baggage()
        {
            cars = new List<BaggageCar>();
            tokens = new ConcurrentDictionary<string, CancellationTokenSource>();
        }


        const string queueFromTimeService = Component.TimeService + Component.Baggage;
        const string queueFromGroundService = Component.GroundService + Component.Baggage;
        const string queueFromGroundMotion = Component.GroundMotion + Component.Baggage;
        const string queueFromStorage = Component.Storage + Component.Baggage;
        const string queueFromAirPlane = Component.Airplane + Component.Baggage;
        const string queueFromVisualizer = Component.Visualizer + Component.Baggage;

        const string queueToAirPlane = Component.Baggage + Component.Airplane;
        const string queueToLogs = Component.Baggage + Component.Logs;
        const string queueToGroundMotion = Component.Baggage + Component.GroundMotion;
        const string queueToGroundService = Component.Baggage + Component.GroundService;
        const string queuetoStorage = Component.Baggage + Component.Storage;
        const string queueToVisualizer = Component.Baggage + Component.Visualizer;
        public RabbitMqClient mqClient { get; set; } = new RabbitMqClient();
        public double timeCoef { get; set; } = 1;
        public string planeID;
        public int baggageCount;
        public string BcarId;
        public int StorVertex;
        public int PlaneVertex;
        public readonly List<string> queues = new List<string>
        {
            queueFromTimeService, queueFromGroundService, queueFromGroundMotion,  queueFromStorage ,queueToAirPlane, queueToLogs, queueToGroundMotion,queueToGroundService, queuetoStorage,queueFromAirPlane
        };


        // ответ 
        void GoPath(GoToVertexAction action, BaggageCar baggageCar, int destinationVertex)
        {
            var path = map.FindShortcut(baggageCar.LocationVertex, destinationVertex);
            for (int i = 0; i < path.Count - 1; i++)
            {
                GoToVertexAlone(baggageCar, path[i + 1]);
            }
        }
        void GoPathHome(GoToVertexAction action, BaggageCar baggageCar, int destinationVertex,
       CancellationToken cancellationToken)
        {
            var path = map.FindShortcut(baggageCar.LocationVertex, destinationVertex);
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                GoToVertexAlone(baggageCar, path[i + 1]);
            }
        }
        void GoToVertexAlone(BaggageCar baggageCar, int DestinationVertex)
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
        void WaitForMotionPermission(BaggageCar baggageCar, int DestinationVertex)
        {
            mqClient.Send<MotionPermissionRequest>(Component.GroundMotion, //permission request
                new MotionPermissionRequest()
                {
                    Action = MotionAction.Occupy,
                    Component = Component.Baggage,
                    DestinationVertex = DestinationVertex,
                    ObjectId = baggageCar.BaggageCarID,
                    StartVertex = baggageCar.LocationVertex
                });

            while (!baggageCar.MotionPermitted)               //check if baggacar can go
                Thread.Sleep(5);
        }
        void MakeAMove(BaggageCar baggageCar, int DestinationVertex)     //just move to vertex
        {
            double position = 0;
            int distance = map.Graph.GetWeightBetweenNearVerties(baggageCar.LocationVertex, DestinationVertex);
            SendVisualizationMessage(baggageCar, DestinationVertex, BaggageCar.Speed);
            while (position < distance)                     //go
            {
                position += BaggageCar.Speed / 3.6 / 1000 * motionInterval * TimeSpeedFactor;
                Thread.Sleep(motionInterval);
            };
            SendVisualizationMessage(baggageCar, DestinationVertex, 0);
            baggageCar.LocationVertex = DestinationVertex;
            baggageCar.MotionPermitted = false;
        }
        void SendVisualizationMessage(BaggageCar baggageCar, int DestinationVertex, int speed)
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




        public void BaggageJob(string planeID, int bagCount)
        {


        }


    }
}
