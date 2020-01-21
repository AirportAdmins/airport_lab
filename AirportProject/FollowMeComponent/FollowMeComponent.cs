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
using System.Collections.Concurrent;
using AirportLibrary.Delay;

namespace FollowMeComponent
{
    delegate void GoToVertexAction(FollowMeCar followme, int DestinationVertex);
    public class FollowMeComponent
    {
        Dictionary<string, string> queuesFrom;
        Dictionary<string, string> queuesTo;
        ConcurrentDictionary<string, FollowMeCar> cars;
        ConcurrentDictionary<string, CancellationTokenSource> tokens;
        RabbitMqClient MqClient;
        Map map = new Map();
        PlayDelaySource source;

        double timeFactor = 1;
        int motionInterval = 100;       //ms
        int countCars = 4;
        public FollowMeComponent()  //TODO sleep в машинке
        {
            MqClient = new RabbitMqClient();
            cars = new ConcurrentDictionary<string, FollowMeCar>();
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
                var followme = new FollowMeCar(i);
                cars.TryAdd(followme.FollowMeId, followme);                
            }
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
                { Component.Visualizer,Component.Visualizer }
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
            {
                timeFactor = mes.Factor;
                source.TimeFactor = timeFactor;
            });
            MqClient.SubscribeTo<AirplaneTransferCommand>(queuesFrom[Component.GroundService], cmd =>//groundservice
                    GotTransferRequest(cmd));
            MqClient.SubscribeTo<MotionPermissionResponse>(queuesFrom[Component.GroundMotion], response => //groundmotion
                    cars[response.ObjectId].MotionPermitted = true);
            MqClient.SubscribeTo<ArrivalConfirmation>(queuesFrom[Component.Airplane], mes =>    //airpane
                    {
                        FollowMeCar followme = null;
                        followme = cars[mes.FollowMeId];
                        if (followme.PlaneId == mes.PlaneId&&followme.LocationVertex==mes.LocationVertex)           
                            followme.GotAirplaneResponse = true;
                    });
        }
        public void GotTransferRequest(AirplaneTransferCommand cmd)
        {
            SendToLogs($"Got transfer command of airplane {cmd.PlaneId} from vertex {cmd.PlaneLocationVertex} "+
                $"to {cmd.DestinationVertex}");
            var followme = cars.Values.FirstOrDefault(car => car.Status == Status.Free);
            while(followme==null)       //waits for a free car
            {
                source.CreateToken().Sleep(100);
                followme = cars.Values.FirstOrDefault(car => car.Status == Status.Free);
            }
              
            if (tokens.TryGetValue(followme.FollowMeId, out var cancellationToken))
            {
                cancellationToken.Cancel();
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
                SendToLogs("Completed servicing airplane ID " + cmd.PlaneId);                
                followme.Status = Status.Free;

                var source = new CancellationTokenSource();     //adds token and remove it after went home/new cmd
                tokens.TryAdd(followme.FollowMeId, source);
                GoPathHome(followme, GetHomeVertex(), source.Token);
                tokens.Remove(followme.FollowMeId, out source);
            });                                                         
        }
        void GoPathHome(FollowMeCar followme, int destinationVertex,
            CancellationToken cancellationToken)
        {
            var path = map.FindShortcut(followme.LocationVertex, destinationVertex);
            for (int i = 0; i < path.Count - 1; i++) 
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                GoToVertexAlone(followme, path[i + 1]);
            }
        }
        void GoPath(GoToVertexAction action, FollowMeCar followme, int destinationVertex)
        {
            var path = map.FindShortcut(followme.LocationVertex, destinationVertex);
            for (int i = 0; i < path.Count - 1; i++)
            {
                action(followme, path[i + 1]);
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
                source.CreateToken().Sleep(10);
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
                source.CreateToken().Sleep(10);
        }
        void MakeAMove(FollowMeCar followme, int DestinationVertex)     //just move to vertex
        {
            double position = 0;
            int distance = map.Graph.GetWeightBetweenNearVerties(followme.LocationVertex, DestinationVertex);
            SendVisualizationMessage(followme, DestinationVertex, FollowMeCar.Speed);
            while (position < distance)                     //go
            {
                position += FollowMeCar.Speed/3.6/1000 * motionInterval * timeFactor;
                source.CreateToken().Sleep(motionInterval);
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
        void SendToLogs(string message)
        {
            MqClient.Send<LogMessage>(queuesTo[Component.Logs], new LogMessage()
            {
                Component = Component.FollowMe,
                Message = message
            });
        }
    }
}
