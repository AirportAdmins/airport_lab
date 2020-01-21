using AirportLibrary;
using AirportLibrary.Delay;
using RabbitMqWrapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DeicingComponent
{
    partial class DaicingComponent
    {
        delegate void GoToVertexAction(DeicingCar baggage, int DestinationVertex);

        PlayDelaySource source;

        ConcurrentDictionary<string, DeicingCar> cars;
        int countOfCars = 6;
        ConcurrentDictionary<string, CancellationTokenSource> tokens;
        Map map = new Map();

        double TimeSpeedFactor = 1;
        int motionInterval = 100;       //ms


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


        public DaicingComponent()
        {
            cars = new ConcurrentDictionary<string, DeicingCar>();
            tokens = new ConcurrentDictionary<string, CancellationTokenSource>();
            mqClient = new RabbitMqClient();
            source = new PlayDelaySource(TimeSpeedFactor);
        }
    }
}
