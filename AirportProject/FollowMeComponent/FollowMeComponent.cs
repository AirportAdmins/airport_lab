﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AirportLibrary;
using RabbitMqWrapper;
using AirportLibrary.DTO;
using System.Threading;
using AirportLibrary.Graph;
using System.Collections.Concurrent;

namespace FollowMeComponent
{
    delegate void GoToVertexAction(FollowMeCar followme, int DestinationVertex);
    public class FollowMeComponent
    {
        Dictionary<string, string> queuesFrom;
        Dictionary<string, string> queuesTo;
        ConcurrentDictionary<string, FollowMeCar> cars;
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
            MqClient.SubscribeTo<MotionPermissionResponse>(queuesFrom[Component.GroundMotion], response => //groundmotion
                    cars[response.ObjectId].MotionPermitted = true);

            MqClient.SubscribeTo<ArrivalConfirmation>(queuesFrom[Component.Airplane], mes =>
                    {
                        FollowMeCar followme = null;
                        followme = cars[mes.FollowMeId];
                        if (followme.PlaneId == mes.PlaneId)            //TODO maybe remove LocationVertex
                            followme.GotAirplaneResponse = true;
                    });
        }
        void GotTransferRequest(AirplaneTransferCommand cmd)
        {
            var followme = cars.Values.First(car => car.Status == Status.Free);
            if (followme == null)
            {
                followme = new FollowMeCar(commonIdCounter);
                followme.LocationVertex = GetHomeVertex();  //to appear in one of vertexes
                commonIdCounter++;
                cars.TryAdd(followme.FollowMeId, followme);  
            }
            followme.Status = Status.Busy;            
            followme.PlaneId = cmd.PlaneId;
            TransferAirplane(followme,cmd).Start();
        }
        Task TransferAirplane(FollowMeCar followme, AirplaneTransferCommand cmd)
        {
            return new Task(() =>
            {     
                GoPath(GoToVertexAlone, followme, cmd.PlaneLocationVertex);
                GoPath(GoToVertexWithAirplane, followme, cmd.DestinationVertex);
                MqClient.Send<ServiceCompletionMessage>(queuesTo[Component.GroundService], new ServiceCompletionMessage()
                {
                    Component = Component.FollowMe,
                    PlaneId = followme.PlaneId
                });
                followme.Status = Status.Free;
                GoPath(GoToVertexAlone, followme, GetHomeVertex());     //TODO will be in another task to cancel 
            });                                                         //if free car will be needed
        }
        void GoPath(GoToVertexAction action, FollowMeCar followme, int destinationVertex)
        {
            var path = map.FindShortcut(followme.LocationVertex, destinationVertex);
            for (int i = 0; i < path.Count - 1; i++) 
            {
                GoToVertexAlone(followme, path[i + 1]);
            }
        }
        int GetHomeVertex()
        {
            List<int> homeVertexes = new List<int>() { 4, 10, 16, 19 };
            Random rand = new Random();
            return homeVertexes.ElementAt(rand.Next(0, 3));
        }
        void GoToVertexWithAirplane(FollowMeCar followme, int DestinationVertex)
        {
            WaitForMotionPermission(followme, DestinationVertex);
            MqClient.Send<FollowMeCommand>(queuesTo[Component.Airplane], new FollowMeCommand()
            {
                FollowMeId = followme.FollowMeId,
                DestinationVertex = DestinationVertex,
                PlaneId = followme.PlaneId
            });
            MakeAMove(followme, DestinationVertex);
            while (!followme.GotAirplaneResponse)           //wait for airplane response
                Thread.Sleep(10);
            MqClient.Send<MotionPermissionRequest>(queuesTo[Component.GroundMotion], //free edge
            new MotionPermissionRequest()
            {
                Action = MotionAction.Free,
                DestinationVertex = DestinationVertex,
                Component = Component.FollowMe,
                ObjectId = followme.FollowMeId,
                StartVertex = followme.LocationVertex
            });
            followme.GotAirplaneResponse = false;
        }
        void GoToVertexAlone(FollowMeCar followme, int DestinationVertex)
        {
            WaitForMotionPermission(followme, DestinationVertex);
            MakeAMove(followme, DestinationVertex);
            MqClient.Send<MotionPermissionRequest>(queuesTo[Component.GroundMotion], //free edge
            new MotionPermissionRequest()
            {
                Action = MotionAction.Free,
                DestinationVertex = DestinationVertex,
                Component = Component.FollowMe,
                ObjectId = followme.FollowMeId,
                StartVertex = followme.LocationVertex
            });
        }
        void WaitForMotionPermission(FollowMeCar followme, int DestinationVertex)
        {
            MqClient.Send<MotionPermissionRequest>(queuesTo[Component.GroundMotion], //permission request
                new MotionPermissionRequest()
                {
                    Action = MotionAction.Occupy,
                    Component = Component.FollowMe,
                    DestinationVertex = DestinationVertex,
                    ObjectId = followme.FollowMeId,
                    StartVertex = followme.LocationVertex
                });

            while (!followme.MotionPermitted)               //check if followme can go
                Thread.Sleep(5);
        }
        void MakeAMove(FollowMeCar followme, int DestinationVertex)     //just move to vertex
        {
            int timeInterval = 10;
            double position = 0;
            int distance = map.Graph.GetWeightBetweenNearVerties(followme.LocationVertex, DestinationVertex);
            SendVisualizationMessage(followme, DestinationVertex, FollowMeCar.Speed);
            while (position < distance)                     //go
            {
                position += FollowMeCar.Speed * timeInterval * TimeSpeedFactor;
                Thread.Sleep(10);
            };
            SendVisualizationMessage(followme, DestinationVertex, 0);
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
        //TODO how they will hide in garage
    }
}
