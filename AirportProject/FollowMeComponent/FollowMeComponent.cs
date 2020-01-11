﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AirportLibrary;
using RabbitMqWrapper;

namespace FollowMeComponent
{
    public class FollowMeComponent
    {
        Dictionary<string, string> queuesFrom;
        Dictionary<string, string> queuesTo;
        RabbitMqClient MqClient;
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
    }
}
