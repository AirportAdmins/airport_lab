using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AirportLibrary;
using RabbitMqWrapper;
using AirportLibrary.DTO;
using System.Threading;
using AirportLibrary.Graph;

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
                    GotTransferRequest(cmd));
            MqClient.SubscribeTo<MotionPermissionResponse>(queuesFrom[Component.GroundMotion], response =>
                    GotMotionResponse(response.ObjectId));
        }
        void GotTransferRequest(AirplaneTransferCommand cmd)
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
        }
        void TransferAirplane(FollowMeCar followme, AirplaneTransferCommand cmd)
        {
            var pathToAirplane = map.FindShortcut(followme.LocationVertex, cmd.PlaneLocationVertex);
            for (int i = 0; i < pathToAirplane.Count - 1; i++)  //go to airplane
            {
                GoToAnotherVertex(followme, pathToAirplane[i + 1]);
            }
            var pathFromAirplane = map.FindShortcut(followme.LocationVertex, cmd.DestinationVertex);
            for (int i = 0; i < pathToAirplane.Count - 1; i++)  //with airplane
            {
                GoToAnotherVertex(followme, pathToAirplane[i + 1]);
            }
        }
        void GotMotionResponse(string FollowMeId)
        {
            cars[FollowMeId].MotionPermitted = true;
        }
        void GoToAnotherVertex(FollowMeCar followme, int DestinationVertex)
        {
            int timeInterval = 10;
            double position = 0;
            int distance = map.Graph.GetWeightBetweenNearVerties(followme.LocationVertex, DestinationVertex);           
            SendVisualizationMessage(followme,DestinationVertex,FollowMeCar.Speed);
            MqClient.Send<MotionPermissionRequest>(queuesTo[Component.GroundMotion], //permission request
                new MotionPermissionRequest()
                {
                    Action = MotionAction.Occupy,
                    Component=Component.FollowMe,
                    DestinationVertex=DestinationVertex,
                    ObjectId=followme.FollowMeId,
                    StartVertex=followme.LocationVertex
                });

            while (!followme.MotionPermitted)               //check if followme can go
                Thread.Sleep(5);
           
            while (position < distance)                     //go
            {
                position += FollowMeCar.Speed * timeInterval * TimeSpeedFactor;
                Thread.Sleep(10);
            };
            SendVisualizationMessage(followme,DestinationVertex,0);
            MqClient.Send<MotionPermissionRequest>(queuesTo[Component.GroundMotion], //free edge
                new MotionPermissionRequest()
                {
                    Action=MotionAction.Free,
                    DestinationVertex=DestinationVertex,
                    Component=Component.FollowMe,
                    ObjectId=followme.FollowMeId,
                    StartVertex=followme.LocationVertex
                });
            followme.LocationVertex = DestinationVertex;
            followme.MotionPermitted = false;         
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
