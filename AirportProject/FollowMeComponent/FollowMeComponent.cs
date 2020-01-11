using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AirportLibrary;
using RabbitMqWrapper;
using AirportLibrary.DTO;
using System.Threading;

namespace FollowMeComponent
{
    public class FollowMeComponent
    {
        Dictionary<string, string> queuesFrom;
        Dictionary<string, string> queuesTo;
        Dictionary<string, FollowMeCar> cars;
        RabbitMqClient MqClient;
        Map map = new Map();

        double TimeSpeedFactor = 1;
        int commonIdCounter = 0;
        public FollowMeComponent()
        {
            MqClient = new RabbitMqClient();
        }
        public void Start()
        {
            CreateQueues();
            DeclareQueues();
        }
        void CreateQueues()
        {
            queuesFrom = new Dictionary<string, string>()
            {
                { Component.GroundMotion,Component.GroundMotion+Component.FollowMe },
                { Component.Airplane,Component.Airplane+Component.FollowMe },
                { Component.GroundService,Component.GroundService+Component.FollowMe },
                { Component.TimeService,Component.TimeService + Component.FollowMe }
            };
            queuesTo = new Dictionary<string, string>()
            {
                { Component.Airplane,Component.FollowMe+Component.Airplane },
                { Component.Logs,Component.Logs+Component.FollowMe },
                { Component.GroundService,Component.FollowMe+Component.GroundService },
                { Component.GroundMotion,Component.FollowMe+Component.GroundMotion },
                { Component.Visualizer,Component.FollowMe+Component.Visualizer }
            };
        }
        void DeclareQueues()
        {
            MqClient.DeclareQueues(queuesFrom.Values.ToArray());
            MqClient.DeclareQueues(queuesTo.Values.ToArray());
        }
        void Subscribe()
        {
            MqClient.SubscribeTo<NewTimeSpeedFactor>(queuesFrom[Component.TimeService], mes =>  //timespeed
                        TimeSpeedFactor = mes.Factor);
            MqClient.SubscribeTo<AirplaneTransferCommand>(queuesFrom[Component.GroundService], cmd =>//groundservice
                    AirplaneTransfer(cmd));
          
        }
        void AirplaneTransfer(AirplaneTransferCommand cmd)
        {
            var followme = cars.Values.First(car => car.Status == Status.Free);
            if (followme == null)
            {
                followme = new FollowMeCar(commonIdCounter);
                commonIdCounter++;
            }
            followme.Status = Status.Busy;
            followme.LocationVertex = 0;    //hz
            followme.PlaneId = cmd.PlaneId;
            followme.PlaneDestinationVertex = cmd.DestinationVertex;
            followme.PlaneLocationVertex = cmd.PlaneLocationVertex;
            followme.Path = map.FindShortcut(followme.LocationVertex, followme.PlaneLocationVertex);
        }
        void GoToAirplane(FollowMeCar followMe)
        {
            for (int i = 0; i < followMe.Path.Count - 1; i++)
            {
                GoToAnotherVertex(followMe, followMe.Path[i + 1]).Wait();
            }
        }
        Task GoToAnotherVertex(FollowMeCar followme, int DestinationVertex)
        {
            int timeInterval = 10;
            double position = 0;
            int distance = 0;                               //get distance from graph
            SendVisualizationMessage(followme,DestinationVertex,FollowMeCar.Speed);
            return new Task (() =>
            {
                while (position < distance)
                {
                    position += FollowMeCar.Speed * timeInterval * TimeSpeedFactor;
                    Thread.Sleep(10);
                };
                SendVisualizationMessage(followme,DestinationVertex,0);
                followme.LocationVertex = DestinationVertex;
            });
        }
        void SendVisualizationMessage(FollowMeCar followme, int DestinationVertex, int speed)
        {
            MqClient.Send<VisualizationMessage>(queuesTo[Component.Visualizer], new VisualizationMessage()
            {
                ObjectId = followme.FollowMeId,
                DestinationVertex = DestinationVertex,
                Speed = speed,
                StartVertex = followme.LocationVertex,
                Type = Component.FollowMe
            });
        }
    }
}
