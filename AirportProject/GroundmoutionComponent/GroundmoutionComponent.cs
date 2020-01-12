using System;
using System.Collections.Generic;
using AirportLibrary.DTO;
using AirportLibrary;
using RabbitMqWrapper;

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
            logger?.Info(String.Format("{0}: Start",ComponentName));

            logger?.Info(String.Format("{0}: DeclareQueues", ComponentName));
            //declare queue SendersToGroundmoution
            mqClient.DeclareQueues(ComponentName);

            //declare queues GroundmoutionToReceivers
            foreach (var receiver in MotionPermissionReceivers)
            {
                mqClient.DeclareQueues(ComponentName + receiver);
            }

            mqClient.SubscribeTo<MotionPermissionRequest>(ComponentName, (message) =>
            {
                lock (lockObj)
                {
                    if (groundmoution.ContainsEdge(message))
                    {
                        switch (message.Action)
                        {
                            case MotionAction.Occupy:
                                if (groundmoution.IsFree(message))
                                {
                                    MotionPermissionResponse response = new MotionPermissionResponse();
                                    response.ObjectId = message.ObjectId;
                                    mqClient.Send<MotionPermissionResponse>(ComponentName + message.Component, response);
                                }
                                groundmoution.Enqueue(message);
                                break;
                            case MotionAction.Free:
                                if (groundmoution.IsFree(message))
                                {
                                    logger?.Error(String.Format("{0}: Component {1} with ID {2} sent moution permisstion response for free queue", ComponentName, message.Component, message.ObjectId));
                                    throw new Exception();
                                }
                                groundmoution.Dequeue(message);
                                if (!groundmoution.IsFree(message))
                                {
                                    var next = groundmoution.Peek(message);
                                    if (next != null)
                                    {
                                        MotionPermissionResponse response = new MotionPermissionResponse();
                                        response.ObjectId = next.ObjectId;
                                        mqClient.Send<MotionPermissionResponse>(ComponentName + next.Component, response);
                                    }
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        logger?.Error(String.Format("{0}: Component {1} with ID {2} sent message for nonexistent edge of map", ComponentName, message.Component, message.ObjectId));
                        throw new Exception();
                    }
                }
            }
            );

        }
    }
}
