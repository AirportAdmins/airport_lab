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
    partial class DeicingComponent
    {
        delegate void GoToVertexAction(DeicingCar baggage, int DestinationVertex);

        PlayDelaySource source;

        ConcurrentDictionary<string, DeicingCar> cars;
        int countOfCars = 6;
        ConcurrentDictionary<string, CancellationTokenSource> tokens;
        Map map = new Map();

        double TimeSpeedFactor = 1;
        int motionInterval = 100;       //ms


        const string queueFromTimeService = Component.TimeService + Component.Deicing;
        const string queueFromGroundService = Component.GroundService + Component.Deicing;
        const string queueFromGroundMotion = Component.GroundMotion + Component.Deicing;

        const string queueToAirPlane = Component.Deicing + Component.Airplane;
        const string queueToLogs = Component.Deicing + Component.Logs;
        const string queueToGroundMotion = Component.Deicing + Component.GroundMotion;
        const string queueToGroundService = Component.Deicing + Component.GroundService;
        const string queueToVisualizer = Component.Deicing + Component.Visualizer;

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
    }
}
