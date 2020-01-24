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
        ConcurrentDictionary<string, Task> carTasks;
        RabbitMqClient MqClient;
        Map map = new Map();
        PlayDelaySource source;

        double timeFactor = 1;
        int motionInterval = 100;       //ms
        int countCars = 4;
        public FollowMeComponent()  
        {
            MqClient = new RabbitMqClient();
            cars = new ConcurrentDictionary<string, FollowMeCar>();
            tokens = new ConcurrentDictionary<string, CancellationTokenSource>();
            source = new PlayDelaySource(timeFactor);
            carTasks = new ConcurrentDictionary<string, Task>();
        }
        public void Start()
        {
            CreateQueues();
            DeclareQueues();
            MqClient.PurgeQueues(queuesFrom.Values.ToArray());
            FillCollections();
            Subscribe();            
        }
        void FillCollections()
        {
            for (int i = 0; i < countCars; i++)
            {
                var followme = new FollowMeCar(i);
                followme.LocationVertex = GetHomeVertex();
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
                { Component.Logs,Component.Logs },
                { Component.GroundService,Component.FollowMe+Component.GroundService },
                { Component.GroundMotion,Component.GroundMotion },
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
                    GotTransferRequest(cmd).Start());
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
        public Task GotTransferRequest(AirplaneTransferCommand cmd)
        {
            Console.WriteLine($"Got transfer command of airplane {cmd.PlaneId} from vertex {cmd.PlaneLocationVertex} " +
                $"to {cmd.DestinationVertex}");
            var followme = cars.Values.FirstOrDefault(car => car.Status == Status.Free);
            Task task = new Task(() =>
            {
                while (followme == null)       //waits for a free car
                {
                    source.CreateToken().Sleep(100);
                    followme = cars.Values.FirstOrDefault(car => car.Status == Status.Free);
                }

                if (tokens.TryGetValue(followme.FollowMeId, out var cancellationToken))
                {
                    cancellationToken.Cancel();                     //cancel going home
                    carTasks[followme.FollowMeId].Wait();           //wait for the task end
                }

                followme.Status = Status.Busy;
                followme.PlaneId = cmd.PlaneId;
                var t = TransferAirplane(followme, cmd);
                carTasks.AddOrUpdate(followme.FollowMeId, t, (key, value) => value = t);  //update task or add if not exists 
                t.Start();
                Console.WriteLine($"FollowMe {followme.FollowMeId} go transfer airplane {cmd.PlaneId}");
            });
            return task;
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
                SendToLogs("Completed transfering airplane ID " + cmd.PlaneId);
                Console.WriteLine($"FollowMe {followme.FollowMeId} completed transfering airplane ID " 
                    + cmd.PlaneId);
                followme.Status = Status.Free;
                Console.WriteLine($"FollowMe {followme.FollowMeId} is free now and going home");
                var source = new CancellationTokenSource();     //adds token and remove it after went home/new cmd
                tokens.TryAdd(followme.FollowMeId, source);
                GoPathHome(followme, GetHomeVertex(), source.Token);
                if (source.Token.IsCancellationRequested)
                    Console.WriteLine($"FollowMe {followme.FollowMeId} is going on new task");
                else
                    Console.WriteLine($"FollowMe {followme.FollowMeId} is in garage now");
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
            return homeVertexes.ElementAt(rand.Next(0, 4));
        }
        void GoToVertexWithAirplane(FollowMeCar followme, int DestinationVertex)
        {
            Console.WriteLine($"FollowMe {followme.FollowMeId} is waiting for motion permission in " +
               $"vertex {followme.LocationVertex} to go to vertex {DestinationVertex}");
            WaitForMotionPermission(followme, DestinationVertex);
            Console.WriteLine($"FollowMe {followme.FollowMeId} got permission to go to {DestinationVertex}");
            var StartVertex = followme.LocationVertex;
            MqClient.Send<FollowMeCommand>(queuesTo[Component.Airplane], new FollowMeCommand()
            {
                FollowMeId = followme.FollowMeId,
                DestinationVertex = DestinationVertex,
                PlaneId = followme.PlaneId
            });
            MakeAMove(followme, DestinationVertex);
            Console.WriteLine($"FollowMe {followme.FollowMeId} is in vertex {DestinationVertex}");
            Console.WriteLine($"FollowMe {followme.FollowMeId} is waiting for airplane " +
                $"{followme.PlaneId} in {followme.LocationVertex}");
            while (!followme.GotAirplaneResponse)           //wait for airplane response
                source.CreateToken().Sleep(10);
            Console.WriteLine($"FollowMe {followme.FollowMeId} got airplane {followme.PlaneId} response staying in {followme.LocationVertex}");
            MqClient.Send<MotionPermissionRequest>(queuesTo[Component.GroundMotion], //free edge
            new MotionPermissionRequest()
            {
                Action = MotionAction.Free,
                DestinationVertex = DestinationVertex,
                Component = Component.FollowMe,
                ObjectId = followme.FollowMeId,
                StartVertex = StartVertex
            });
            followme.GotAirplaneResponse = false;
        }
        void GoToVertexAlone(FollowMeCar followme, int DestinationVertex)
        {
            Console.WriteLine($"FollowMe {followme.FollowMeId} is waiting for motion permission in " +
                $"vertex {followme.LocationVertex} to go to vertex {DestinationVertex}");
            WaitForMotionPermission(followme, DestinationVertex);
            Console.WriteLine($"FollowMe {followme.FollowMeId} got permission to go to {DestinationVertex}");
            var startVertex = followme.LocationVertex;
            MakeAMove(followme, DestinationVertex);
            Console.WriteLine($"FollowMe {followme.FollowMeId} is in vertex {DestinationVertex}");            
            MqClient.Send<MotionPermissionRequest>(queuesTo[Component.GroundMotion], //free edge
            new MotionPermissionRequest()
            {
                Action = MotionAction.Free,
                DestinationVertex = DestinationVertex,
                Component = Component.FollowMe,
                ObjectId = followme.FollowMeId,
                StartVertex = startVertex
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
            int distance = map.Graph.GetWeightBetweenNearVerties(followme.LocationVertex, DestinationVertex);
            SendVisualizationMessage(followme, DestinationVertex, FollowMeCar.Speed);
            source.CreateToken().Sleep(distance * 1000 / FollowMeCar.Speed);
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
