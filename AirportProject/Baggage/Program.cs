using System;
using RabbitMqWrapper;
using AirportLibrary;
using AirportLibrary.DTO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using AirportLibrary.Graph;
using System.Collections.Concurrent;

namespace Baggage
{
    class Program
    {
        static void Main(string[] args)
        {
            Baggage b = new Baggage();
            b.Start();
        }
        

        
    }
}
