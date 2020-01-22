using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Linq;
using RabbitMqWrapper;
using AirportLibrary;
using AirportLibrary.DTO;
using AirportLibrary.Delay;
using AirportLibrary.Graph;
using System.Threading;
using System.Threading.Tasks;

namespace FuelTruck
{
    delegate void GoToVertexAction(FuelTruckCar fueltruck, int DestinationVertex);
    public class FuelTruck
    {
        Dictionary<string, string> queuesFrom;
        Dictionary<string, string> queuesTo;
        ConcurrentDictionary<string, FollowMeCar> cars;
        ConcurrentDictionary<string, CancellationTokenSource> tokens;

        double timeFactor = 1;
        int motionInterval = 100;       //ms
        int countCars = 4;

        RabbitMqClient MqClient;
        Map map = new Map();
        PlayDelaySource source;

        public FuelTruck()
        {
            MqClient = new RabbitMqClient();
            cars = new ConcurrentDictionary<string, FuelTruckCar>();
            tokens = new ConcurrentDictionary<string, CancellationTokenSource>();
            source = new PlayDelaySource(timeFactor);
        }

        public void Start()
        {
            CreateQueues();
            DeclareQueues();
            MqClient.PurgeQueues(queuesFrom.Values.ToArray());
            Subscribe();
            FillCollections();
        }
        void FillCollections()
        {
            for (int i = 0; i < countCars; i++)
            {
                var fueltruck = new FuelTruckCar(i);
                cars.TryAdd(fueltruck.FuelTrackId, fueltruck);
            }
        }

        void CreateQueues()
        {
            queuesFrom = new Dictionary<string, string>()
            {
                { Component.GroundMotion,Component.GroundMotion+Component.FuelTruck },
                { Component.Airplane,Component.Airplane+Component.FuelTruck },
                { Component.GroundService,Component.GroundService+Component.FuelTruck },
                { Component.TimeService,Component.TimeService + Component.FuelTruck }
            };
            queuesTo = new Dictionary<string, string>()
            {
                { Component.Airplane,Component.FuelTruck+Component.Airplane },
                { Component.Logs,Component.Logs+Component.FuelTruck },
                { Component.GroundService,Component.FuelTruck+Component.GroundService },
                { Component.GroundMotion,Component.FuelTruck+Component.GroundMotion },
                { Component.Visualizer,Component.Visualizer }
            };
        }

        void DeclareQueues()
        {
            MqClient.DeclareQueues(queuesFrom.Values.ToArray());
            MqClient.DeclareQueues(queuesTo.Values.ToArray());
        }




    }
}
