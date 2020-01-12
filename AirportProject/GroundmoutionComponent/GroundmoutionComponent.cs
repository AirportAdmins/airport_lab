using System;
using System.Collections.Generic;
using AirportLibrary.DTO;
using AirportLibrary;
using RabbitMqWrapper;

namespace GroundmoutionComponent
{
    public class GroundmoutionComponent
    {
        public static readonly string SendersToGroundmoutionQueue = Component.GroundMotion;

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

        RabbitMqClient mqClient = new RabbitMqClient();

        GroundmoutionQueues groundmoution = new GroundmoutionQueues();

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
                mqClient.DeclareQueues(receiver + Component.GroundMotion);
            }

            mqClient.SubscribeTo<MotionPermissionRequest>(SendersToGroundmoutionQueue, )

        }
    }
}
