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
        Random rand = new Random();

        public TransportMotion(string Component,RabbitMqClient MqClient,PlayDelaySource source)
        {
            this.component = Component;
            this.mqClient = MqClient;
            this.source = source;
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
                { Component.Logs, Component.Logs },
                { Component.GroundMotion,Component.GroundMotion },
                { Component.Visualizer,Component.Visualizer },
            };
        }
  
        void Subscribe()
        {
            mqClient.SubscribeTo<NewTimeSpeedFactor>(queuesFrom[Component.TimeService], mes =>  //timespeed
            {
                timeFactor = mes.Factor;
                source.TimeFactor = timeFactor;
                });       
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

            Console.WriteLine($"{component}car is waiting for motion permission");
            WaitForMotionPermission(car, StartVertex,DestinationVertex);
            SendVisualizationMessage(car, StartVertex,DestinationVertex, car.Speed);
            Console.WriteLine($"{component}car is going to vertex {DestinationVertex}");
            while (position < distance)                     //go
            {
                position += car.Speed / 3.6 / 1000 * motionInterval * timeFactor;
                source.CreateToken().Sleep(motionInterval);
            };
            car.LocationVertex = DestinationVertex;         //change location
<<<<<<< HEAD

=======
>>>>>>> dfdbc71d5b2c9c12c1dfe4e2a73c2d708af3a952
            car.MotionPermitted = false;
            Console.WriteLine($"{component}car is in vertex {DestinationVertex}");
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
                source.CreateToken().Sleep(5);
        }

        public int GetHomeVertex()
        {
            List<int> homeVertexes = new List<int>() { 4, 10, 16, 19 };
            Random rand = new Random();
            return homeVertexes.ElementAt(rand.Next(0, 4));
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
