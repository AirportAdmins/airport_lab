using System;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using AirportLibrary.DTO;
using AirportLibrary;

namespace LogsComponent
{
    public class MyMessage
    {
        public string message;
    }
    class Program
    {
        static void Main(string[] args)
        {
            RabbitMqWrapper.RabbitMqClient client = new RabbitMqWrapper.RabbitMqClient();
            LogsComponent logs = new LogsComponent();
            logs.Start(client);
            
            client.Send<LogMessage>("logs", new LogMessage()
            {
                Component=Component.Airplane,
                Message="airplane mazafaka"
            });
            client.Send<LogMessage>("logs", new LogMessage()
            {
                Component = Component.FollowMe,
                Message = "go to vertex alone"
            });
            client.Send<LogMessage>("logs", new LogMessage()
            {
                Component = Component.Airplane,
                Message = "aaa"
            });
        }
        
    }
}
