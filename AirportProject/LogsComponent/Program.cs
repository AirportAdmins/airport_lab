//using Microsoft.Extensions.Logging;
using System;
using Loggly;
using Loggly.Config;
using NLog;
using NLog.Extensions.Logging;


namespace LogsComponent
{
    class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            var config = LogglyConfig.Instance;
            config.CustomerToken = "39065107-bd7c-4988-bd93-201709d0cf62";
            config.ApplicationName = $"MyApp-airport";

            config.Transport.EndpointHostname = "logs-01.loggly.com";
            config.Transport.EndpointPort = 443;
            config.Transport.LogTransport = LogTransport.Https;

            var ct = new ApplicationNameTag();
            ct.Formatter = "application-{0}";
            config.TagConfig.Tags.Add(ct);
            ILogglyClient _loggly = new LogglyClient();
            var logEvent = new LogglyEvent();
            logEvent.Data.Add("message", "Simple message at {0}", DateTime.Now);
            _loggly.Log(logEvent);
        }
        
    }
}
