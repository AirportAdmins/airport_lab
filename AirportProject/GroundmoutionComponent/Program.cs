using System;

namespace GroundmoutionComponent
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger logger = new Logger();
            logger.Write += Console.WriteLine;
            new GroundmoutionComponent(logger).Start();
        }
    }
}
