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
        public static readonly string SendersToGroundmoutionQueue = ComponentName;

        public static readonly List<string> MotionPermissionSenders = new List<string>()
        {
            Component.Airplane,
            Component.Bus,
            Component.Catering,
            Component.Deicing,
            Component.FollowMe,
            Component.FuelTruck
        };

        public static readonly List<string> MotionPermissionReceivers = MotionPermissionSenders;

        private readonly object lockObj = new object();

        RabbitMqClient mqClient = new RabbitMqClient();

        GroundmoutionQueue groundmoution = new GroundmoutionQueue();

        public GroundmoutionComponent()
        {
        }
        public void Start()
        {
            //declare queue SendersToGroundmoution
            mqClient.DeclareQueues(SendersToGroundmoutionQueue);

            //declare queues GroundmoutionToReceivers
            foreach (var receiver in MotionPermissionReceivers)
            {
                mqClient.DeclareQueues(ComponentName + receiver);
            }

            mqClient.SubscribeTo<MotionPermissionRequest>(SendersToGroundmoutionQueue, (message) =>
            {
                lock (lockObj)
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
                            groundmoution.Dequeue(message);
                            if (!groundmoution.IsFree(message))
                            {
                                var next = groundmoution.Peek(message);
                                MotionPermissionResponse response = new MotionPermissionResponse();
                                response.ObjectId = next.ObjectId;
                                mqClient.Send<MotionPermissionResponse>(ComponentName + next.Component, response);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            );

        }
    }
}
