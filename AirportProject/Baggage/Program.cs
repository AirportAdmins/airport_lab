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

namespace BaggageComponent
{
    public enum Status
    {
        Free, Busy
    }
    public class BaggageCar
    {
        string baggageCarID;
        public BaggageCar(int id)
        {
            baggageCarID = "Baggage-" + id;
            MotionPermitted = false;
            GotAirplaneResponse = false;
    
        }
        public string BaggageCarID { get => baggageCarID; }
        public string PlaneId { get; set; }
        public Status Status { get; set; }

        //motion data
        public bool MotionPermitted { get; set; }
        public bool GotAirplaneResponse { get; set; }
       //
        public static int Speed { get => 50; }
        public int LocationVertex { get; set; }
    }

        delegate void GoToVertexAction(BaggageCar baggage, int DestinationVertex);
    
    class Baggage
    {
        
        ConcurrentDictionary<string, BaggageCar> cars;
        ConcurrentDictionary<string, CancellationTokenSource> tokens;
        Map map = new Map();

        double TimeSpeedFactor = 1;
        int commonIdCounter = 0;
        int motionInterval = 100;       //ms
        public Baggage()
        {
            cars = new ConcurrentDictionary<string, BaggageCar>();
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

            bg.mqClient.SubscribeTo<BaggageFromStorageResponse>(queueFromStorage, (mes) =>
           {
               bg.BcarId = mes.BaggageCarId;
               bg.baggageCount = mes.BaggageCount;
           });

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
            bg.mqClient.SubscribeTo<MotionPermissionResponse>(queueFromGroundMotion, response => //groundmotion
                    cars[response.ObjectId].MotionPermitted = true);
        }
        // ответ 
        void GoPath(GoToVertexAction action, BaggageCar followme, int destinationVertex)
        {
            var path = map.FindShortcut(followme.LocationVertex, destinationVertex);
            for (int i = 0; i < path.Count - 1; i++)
            {
                GoToVertexAlone(followme, path[i + 1]);
            }
        }
        void GoPathHome(GoToVertexAction action, BaggageCar baggageme, int destinationVertex,
       CancellationToken cancellationToken)
        {
            var path = map.FindShortcut(baggageme.LocationVertex, destinationVertex);
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                GoToVertexAlone(baggageme, path[i + 1]);
            }
        }
        void GoToVertexAlone(BaggageCar baggageme, int DestinationVertex)
        {
            WaitForMotionPermission(baggageme, DestinationVertex);
            MakeAMove(baggageme, DestinationVertex);
            mqClient.Send<MotionPermissionRequest>(queueToGroundMotion, //free edge
            new MotionPermissionRequest()
            {
                Action = MotionAction.Free,
                DestinationVertex = DestinationVertex,
                Component = Component.Baggage,
                ObjectId = baggageme.BaggageCarID,
                StartVertex = baggageme.LocationVertex
            });
        }
        void WaitForMotionPermission(BaggageCar baggageme, int DestinationVertex)
        {
            mqClient.Send<MotionPermissionRequest>(Component.GroundMotion, //permission request
                new MotionPermissionRequest()
                {
                    Action = MotionAction.Occupy,
                    Component = Component.Baggage,
                    DestinationVertex = DestinationVertex,
                    ObjectId = baggageme.BaggageCarID,
                    StartVertex = baggageme.LocationVertex
                });

            while (!baggageme.MotionPermitted)               //check if baggacar can go
                Thread.Sleep(5);
        }
        void MakeAMove(BaggageCar baggage, int DestinationVertex)     //just move to vertex
        {
            double position = 0;
            int distance = map.Graph.GetWeightBetweenNearVerties(baggage.LocationVertex, DestinationVertex);
            SendVisualizationMessage(baggage, DestinationVertex, BaggageCar.Speed);
            while (position < distance)                     //go
            {
                position += BaggageCar.Speed / 3.6 / 1000 * motionInterval * TimeSpeedFactor;
                Thread.Sleep(motionInterval);
            };
            SendVisualizationMessage(baggage, DestinationVertex, 0);
            baggage.LocationVertex = DestinationVertex;
            baggage.MotionPermitted = false;
        }
        void SendVisualizationMessage(BaggageCar baggeme, int DestinationVertex, int speed)
        {
            mqClient.Send<VisualizationMessage>(queueToVisualizer, new VisualizationMessage()
            {
                ObjectId = baggeme.BaggageCarID,
                DestinationVertex = DestinationVertex,
                Speed = speed,
                StartVertex = baggeme.LocationVertex,
                Type = Component.Baggage
            });
        }




        public void BaggageJob(string planeID, int bagCount)
        {
            

        }

        
    }
}
