using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AirplaneComponent
{
    class Program
    {
        static void Main(string[] args)
        {
            new AirplaneComponent().Start();
        }
    }
}
