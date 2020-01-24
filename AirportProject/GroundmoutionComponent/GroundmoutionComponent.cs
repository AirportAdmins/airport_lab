using System;
using System.Collections.Generic;
using AirportLibrary.DTO;
using AirportLibrary;
using RabbitMqWrapper;
using System.Threading.Tasks;

namespace GroundmoutionComponent
{
    public class GroundmoutionComponent
    {
        public static readonly string ComponentName = Component.GroundMotion;

        public static readonly List<string> MotionPermissionReceivers = new List<string>()
        {
            Component.Airplane,
            Component.Bus,
            Component.Catering,
            Component.Deicing,
            Component.FollowMe,
            Component.FuelTruck
        };

        private readonly object lockObj = new object();

        RabbitMqClient mqClient = new RabbitMqClient();

        GroundmoutionQueue groundmoution = new GroundmoutionQueue();

        ILogger logger;

        public GroundmoutionComponent(ILogger logger)
        {
            this.logger = logger;
        }

        public void Start()
        {
            logger?.Info($"{ComponentName}: Start");

            //declare queue SendersToGroundmoution
            mqClient.DeclareQueues(ComponentName);
            mqClient.PurgeQueues(ComponentName);

            //declare queues GroundmoutionToReceivers
            foreach (var receiver in MotionPermissionReceivers)
            {
                mqClient.DeclareQueues(ComponentName + receiver);
                mqClient.PurgeQueues(ComponentName + receiver);
            }

            logger?.Info($"{ComponentName}: Queues declared");

            mqClient.SubscribeTo<MotionPermissionRequest>(ComponentName, (message) =>
            {
                Task.Run(()=> {
                    logger?.Info($"{ComponentName}: {message.Component} with Id {message.ObjectId} sent message with Action {((message.Action == MotionAction.Free) ? "Free" : "Occupy")} on edge {message.StartVertex}-{message.DestinationVertex}");

                    lock (lockObj)
                    {
                        if (groundmoution.ContainsEdge(message))
                        {
                            switch (message.Action)
                            {
                                case MotionAction.Occupy:
                                    if (groundmoution.IsFree(message))
                                    {
                                        logger?.Debug($"{ComponentName}: Edge {message.StartVertex}-{message.DestinationVertex} is free");

                                        mqClient.Send<MotionPermissionResponse>(ComponentName + message.Component, new MotionPermissionResponse() { ObjectId = message.ObjectId });

                                        logger?.Info($"{ComponentName}: Sent to {message.Component} with Id {message.ObjectId} permission");
                                    }
                                    else
                                        logger?.Debug($"{ComponentName}: Edge {message.StartVertex}-{message.DestinationVertex} is not free");

                                    groundmoution.Enqueue(message);

                                    logger?.Debug($"{ComponentName}: {message.Component} with Id {message.ObjectId} stood in queue");
                                    break;
                                case MotionAction.Free:
                                    if (groundmoution.IsFree(message))
                                    {
                                        logger?.Error($"{ComponentName}: {message.Component} with Id {message.ObjectId} sent that edge is free but queue has been alredy free");

                                        throw new Exception();
                                    }
                                    groundmoution.Dequeue(message);

                                    logger?.Debug($"{ComponentName}: {message.Component} with Id {message.ObjectId} came out from queue");

                                    if (!groundmoution.IsFree(message))
                                    {
                                        var next = groundmoution.Peek(message);
                                        mqClient.Send<MotionPermissionResponse>(ComponentName + next.Component, new MotionPermissionResponse() { ObjectId = next.ObjectId });

                                        logger?.Info($"{ComponentName}: Sent to {next.Component} with Id {next.ObjectId} permission");
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            logger?.Error($"{ComponentName}: Component {message.Component} with ID {message.ObjectId} sent message for nonexistent edge of map");
                            throw new Exception();
                        }
                    }
                });
            }
            );

        }
    }
}
