using System;
using System.Collections.Generic;
using System.Text;

namespace Baggage
{
    class RandomHomeVertex
    {
        private static Random rand = new Random();

        private static List<int> homeVertexes = new List<int>() { 4, 10, 16, 19 };

        public static int GetHomeVertex()
        {
            return homeVertexes[rand.Next(0, 4)];
        }
    }
}
