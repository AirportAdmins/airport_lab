using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace AirportLibrary.Config
{
    public static class ConfigManager
    {
        public static string HostName
        {
            get => ConfigurationManager.AppSettings["hostname"];
        }
        public static string UserName
        {
            get => ConfigurationManager.AppSettings["username"];
        }
        public static string Password {
            get => ConfigurationManager.AppSettings["password"];
        }
    }
}
