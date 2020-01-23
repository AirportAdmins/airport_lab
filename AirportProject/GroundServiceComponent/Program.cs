using System;

namespace GroundServiceComponent
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger logger = new Logger();
            logger.Write += Console.WriteLine;
            new GroundServiceComponent(logger).Start();
        }
    }
}
