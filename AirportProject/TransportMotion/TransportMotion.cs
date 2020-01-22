using System;
using System.Collections.Generic;
using System.Text;
using RabbitMqWrapper;
using AirportLibrary;
using System.Linq;
using AirportLibrary.DTO;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AirportLibrary.Delay;

namespace TransportMotion
{
    public class TransportMotion
    {
        RabbitMqClient mqClient;
        string component;
        Dictionary<string, string> queuesFrom;
        Dictionary<string, string> queuesTo;
        Map map = new Map();
        double timeFactor;
        int motionInterval = 100;
        PlayDelaySource source;

        public TransportMotion(string Component,RabbitMqClient MqClient)
        {
            this.component = Component;
            this.mqClient = MqClient;
            source = new PlayDelaySource(timeFactor);
            CreateQueues();
            Subscribe();
        }
        void CreateQueues()
        {
            queuesFrom = new Dictionary<string, string>()
            {
                { Component.TimeService,Component.TimeService + component },
            };
            queuesTo = new Dictionary<string, string>()
            {
                { Component.Logs,component+Component.FollowMe },
                { Component.GroundMotion,component+Component.GroundMotion },
                { Component.Visualizer,component+Component.Visualizer },
            };
        }
 
        void Subscribe()
        {
            mqClient.SubscribeTo<NewTimeSpeedFactor>(queuesFrom[Component.TimeService], mes =>  //timespeed
                    timeFactor = mes.Factor);       
        }
       
        public void GoPath(ICar car, int destinationVertex)     
        {
            var path = map.FindShortcut(car.LocationVertex, destinationVertex);
            for (int i = 0; i < path.Count - 1; i++)
            {
                GoToVertex(car, path[i + 1]);
            }
        }
        public void GoPathFree(ICar car, int destinationVertex, CancellationToken token) 
        {
            var path = map.FindShortcut(car.LocationVertex, destinationVertex);
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (token.IsCancellationRequested)
                    break;
                GoToVertex(car, path[i + 1]);
            }
        }
        void GoToVertex(ICar car, int DestinationVertex)
        {            
            double position = 0;
            int distance = map.Graph.GetWeightBetweenNearVerties(car.LocationVertex, DestinationVertex);
            int StartVertex = car.LocationVertex;
            WaitForMotionPermission(car, StartVertex,DestinationVertex);
            SendVisualizationMessage(car, StartVertex,DestinationVertex, car.Speed);
            while (position < distance)                     //go
            {
                position += car.Speed / 3.6 / 1000 * motionInterval * timeFactor;
                Thread.Sleep(motionInterval);
            };
            car.LocationVertex = DestinationVertex;         //change location
            car.MotionPermitted = false;
            SendVisualizationMessage(car, StartVertex, DestinationVertex, 0);           
            mqClient.Send<MotionPermissionRequest>(queuesTo[Component.GroundMotion], //free edge
            new MotionPermissionRequest()
            {
                Action = MotionAction.Free,
                DestinationVertex = DestinationVertex,
                Component = component,
                ObjectId = car.CarId,
                StartVertex = StartVertex
            });          
        }
        void WaitForMotionPermission(ICar car, int StartVertex,int DestinationVertex)
        {
            mqClient.Send<MotionPermissionRequest>(queuesTo[Component.GroundMotion], //permission request
                new MotionPermissionRequest()
                {
                    Action = MotionAction.Occupy,
                    Component = component,
                    DestinationVertex = DestinationVertex,
                    ObjectId = car.CarId,
                    StartVertex = StartVertex
                });

            while (!car.MotionPermitted)               //check if car can go
                Thread.Sleep(5);
        }

        public int GetHomeVertex()
        {
            List<int> homeVertexes = new List<int>() { 4, 10, 16, 19 };
            Random rand = new Random();
            return homeVertexes.ElementAt(rand.Next(0, 3));
        }
        void SendVisualizationMessage(ICar car, int StartVertex, int DestinationVertex, int speed)
        {
            mqClient.Send<VisualizationMessage>(queuesTo[Component.Visualizer], new VisualizationMessage()
            {
                ObjectId = car.CarId,
                DestinationVertex = DestinationVertex,
                Speed = speed,
                StartVertex = StartVertex,
                Type = component
            });
        }
    }
}
