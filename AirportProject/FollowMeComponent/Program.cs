using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AirportLibrary.Delay;

namespace FollowMeComponent
{
    class Program
    {
        static void Main(string[] args)
        {
            //PlayDelaySource source = new PlayDelaySource(1);
            //var token = source.CreateToken();
            //token.Sleep(10000);
            new FollowMeComponent().Start();
        }
    }
}
