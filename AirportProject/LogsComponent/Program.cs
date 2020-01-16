//using Microsoft.Extensions.Logging;
using System;
using NLog;

namespace LogsComponent
{
    class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {

            //try
            //{
            //    Logger.Info("Hello world");
            //    System.Console.ReadKey();
            //}
            //catch (Exception ex)
            //{
            //    Logger.Error(ex, "Goodbye cruel world");
            //}
            //NLog.LogManager.Shutdown();
            int Id = 1;
            Cout($"Passenger {0} has come too early for flight  registration",Id);
        }
        static void Cout(string s,int Id)
        {
            Console.WriteLine(s+Id);
        }
    }
}
