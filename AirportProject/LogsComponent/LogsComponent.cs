using System;
using System.Collections.Generic;
using System.Text;
using AirportLibrary;
using System.Linq;
using RabbitMqWrapper;
using System.Collections.Concurrent;
using AirportLibrary.DTO;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;


namespace LogsComponent
{
    public class LogsComponent
    {
        RabbitMqClient MqClient;
        Dictionary<string, string> queuesFrom;
        DateTime playTime = DateTime.Now;
        Logger logger;
        public void Start(RabbitMqClient client)
        {
            LogManager.Configuration = new XmlLoggingConfiguration("NLog.config");
            logger = LogManager.GetCurrentClassLogger();
            MqClient = client;
            CreateQueues();
            DeclareQueues();
            MqClient.PurgeQueues(queuesFrom.Values.ToArray());
            Subscribe();
        }
        void CreateQueues()
        {
            queuesFrom = new Dictionary<string, string>()
            {
                {Component.Airplane, Component.Logs+"for"+Component.Airplane+"service"},
                {Component.Logs, "logs"},
                {Component.TimeService,Component.TimeService+Component.Logs }
            };
        }
        void Subscribe()
        {
        
            MqClient.SubscribeTo<LogMessage>("logs", mes => Log(mes));

            MqClient.SubscribeTo<CurrentPlayTime>(queuesFrom[Component.TimeService], mes =>
                 playTime = mes.PlayTime);
        }
        void Log(LogMessage mes)
        {
            logger.Info(() => playTime.TimeOfDay + " " + mes.Component + ": " + mes.Message);
        }
        void DeclareQueues()
        {
            MqClient.DeclareQueues(queuesFrom.Values.ToArray());
        }
    }
}
